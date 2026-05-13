using Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

public class NewSkillSystemDefaultSkillCastFlow : ISkillCastFlow
{
    private readonly INewSkillSystemSkillTargetingPreviewService _targetingPreview;

    private SkillDataSO _currentSkill;
    private GameObject _currentPreview;
    private Transform _fallbackTarget;

    public NewSkillSystemDefaultSkillCastFlow(INewSkillSystemSkillTargetingPreviewService targetingPreview) {
        _targetingPreview = targetingPreview;
    }

    public bool CanHandleCaster(IPlayableUnit caster)
    {
        if (caster == null || caster.UnitViewGO == null) return false;

        var legacyToggle = caster.UnitViewGO.GetComponent<LegacySkillSystemToggle>();
        if (legacyToggle != null && legacyToggle.UseLegacySkillSystem) return false;

        var loadout = caster.UnitViewGO.GetComponent<NewSkillSystemSkillLoadout>();
        return loadout != null;
    }

    public void InitEntryPoint(INaraController naraController)
    {
        // No setup required for ScriptableObject-driven casts.
    }

    public bool TryPrepareCast(int index, IPlayableUnit caster, out SkillCastPrepareResult prepareResult)
    {
        prepareResult = default;
        if (caster == null || caster.UnitViewGO == null) return false;

        var legacyToggle = caster.UnitViewGO.GetComponent<LegacySkillSystemToggle>();
        if (legacyToggle != null && legacyToggle.UseLegacySkillSystem) return false;

        var loadout = caster.UnitViewGO.GetComponent<NewSkillSystemSkillLoadout>();
        if (loadout == null) return false;
        if (!loadout.TryGetSkill(index, out SkillDataSO skill)) return false;
        if (!skill.IsCastable) return false;

        _currentSkill = skill;
        Transform spawn = caster.UnitSkillSpotTransform != null ? caster.UnitSkillSpotTransform : caster.UnitViewGO.transform;
        if (_currentSkill.CastType == SkillCastType.Area && _currentSkill.AreaAimPrefab != null)
            _currentPreview = Object.Instantiate(_currentSkill.AreaAimPrefab, spawn.position, spawn.rotation);
        else if (_currentSkill.CastType == SkillCastType.Projectile && _currentSkill.ProjectileAimPrefab != null)
            _currentPreview = Object.Instantiate(_currentSkill.ProjectileAimPrefab, spawn.position, spawn.rotation);
        else if (_currentSkill.CastType == SkillCastType.Self && _currentSkill.SelfAimPrefab != null)
        {
            Vector3 foot = NewSkillSystemSkillAimWorld.GetSelfCastFootWorld(caster);
            _currentPreview = Object.Instantiate(_currentSkill.SelfAimPrefab, foot, caster.UnitViewGO.transform.rotation);
        }

        EnsureFallbackTarget(caster);
        UpdateFallbackTarget(caster);

        _targetingPreview?.Begin(_currentSkill, caster, _currentPreview != null ? _currentPreview.transform : null);

        prepareResult = new SkillCastPrepareResult
        {
            AbilityIndex = index,
            Cost = _currentSkill.SkillType == Logic.Scripts.GameDomain.Services.Skills.SkillType.Passive ? 0 : Mathf.Max(0, _currentSkill.Cost),
            AnimatorAttackType = index + 1
        };
        return true;
    }

    public void ExecutePreparedCast(IPlayableUnit caster)
    {
        if (_currentSkill == null || caster == null) return;

        UpdateFallbackTarget(caster);
        _targetingPreview?.End();

        if (_currentSkill.CastType == SkillCastType.Area && _currentSkill.AreaImpactPrefab != null && _fallbackTarget != null)
            Object.Instantiate(_currentSkill.AreaImpactPrefab, _fallbackTarget.position, Quaternion.identity);
        else if (_currentSkill.CastType == SkillCastType.Self && _currentSkill.SelfCastPrefab != null)
        {
            Vector3 p = NewSkillSystemSkillAimWorld.GetSelfCastFootWorld(caster);
            Object.Instantiate(_currentSkill.SelfCastPrefab, p, caster.UnitViewGO != null ? caster.UnitViewGO.transform.rotation : Quaternion.identity);
        }

        Transform castTarget = _fallbackTarget != null ? _fallbackTarget : _currentPreview != null ? _currentPreview.transform : caster.UnitViewGO.transform;
        _currentSkill.OnCast(caster, castTarget);
        CleanupPreviewAndTarget();
        _currentSkill = null;
    }

    public void CancelPreparedCast(IPlayableUnit caster)
    {
        _targetingPreview?.End();
        CleanupPreviewAndTarget();
        _currentSkill = null;
    }

    private void EnsureFallbackTarget(IPlayableUnit caster)
    {
        if (_fallbackTarget != null) return;
        var go = new GameObject("NewSkillSystemCastTarget");
        _fallbackTarget = go.transform;
        if (caster?.UnitViewGO != null)
            _fallbackTarget.position = caster.UnitViewGO.transform.position;
    }

    private void UpdateFallbackTarget(IPlayableUnit caster)
    {
        if (_fallbackTarget == null || caster == null || caster.UnitViewGO == null) return;

        Vector3 origin = caster.UnitSkillSpotTransform != null ? caster.UnitSkillSpotTransform.position : caster.UnitViewGO.transform.position;
        Vector3 fallbackForward = caster.UnitViewGO.transform.forward;
        Vector3 point;
        if (_currentSkill != null && _currentSkill.CastType == SkillCastType.Self)
        {
            point = NewSkillSystemSkillAimWorld.GetSelfCastFootWorld(caster);
        }
        else if (_currentSkill != null && _currentSkill.CastType == SkillCastType.Area)
        {
            point = NewSkillSystemSkillAimWorld.GetAreaClampedAimPoint(caster, caster, _currentSkill);
        }
        else
        {
            point = TryGetMouseWorldPoint(out Vector3 worldPoint) ? worldPoint : (origin + fallbackForward * 2f);
            point = NewSkillSystemSkillAimWorld.ClampDirectedEnd(origin, point, _currentSkill != null ? _currentSkill.GetProjectileRange() : 500f);
        }

        _fallbackTarget.position = point;
        Vector3 direction = point - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude < 1e-6f) {
            direction = new Vector3(fallbackForward.x, 0f, fallbackForward.z);
        }
        if (direction.sqrMagnitude > 1e-6f)
            _fallbackTarget.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static bool TryGetMouseWorldPoint(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (Camera.main == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo)) return false;

        worldPoint = hitInfo.point;
        return true;
    }

    private void CleanupPreviewAndTarget()
    {
        if (_currentPreview != null)
        {
            Object.Destroy(_currentPreview);
            _currentPreview = null;
        }

        if (_fallbackTarget != null)
        {
            Object.Destroy(_fallbackTarget.gameObject);
            _fallbackTarget = null;
        }
    }
}
