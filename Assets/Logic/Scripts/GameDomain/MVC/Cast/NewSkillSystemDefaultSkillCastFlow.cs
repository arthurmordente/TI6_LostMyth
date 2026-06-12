using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem;
using Logic.Scripts.GameDomain.MVC.Environment;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

public class NewSkillSystemDefaultSkillCastFlow : ISkillCastFlow
{
    private readonly INewSkillSystemSkillTargetingPreviewService _targetingPreview;
    private readonly INaraController _nara;

    private SkillDataSO _currentSkill;
    private GameObject _currentPreview;
    private Transform _fallbackTarget;

    public NewSkillSystemDefaultSkillCastFlow(
        INewSkillSystemSkillTargetingPreviewService targetingPreview,
        INaraController nara)
    {
        _targetingPreview = targetingPreview;
        _nara = nara;
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
        SkillCastPresentationTarget.Bind(naraController ?? _nara);
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
        {
            Vector3 castOrigin = NewSkillSystemSkillAimWorld.GetSkillOrigin(caster, caster);
            _currentPreview = Object.Instantiate(_currentSkill.ProjectileAimPrefab, castOrigin, Quaternion.identity);
        }
        else if (_currentSkill.CastType == SkillCastType.Self && _currentSkill.SelfAimPrefab != null)
        {
            Vector3 foot = SkillCastPresentationTarget.GetSelfCastFootWorld(caster, _currentSkill);
            Transform presentation = SkillCastPresentationTarget.GetSelfCastTransform(caster, _currentSkill);
            Quaternion rotation = presentation != null ? presentation.rotation : caster.UnitViewGO.transform.rotation;
            _currentPreview = Object.Instantiate(_currentSkill.SelfAimPrefab, foot, rotation);
        }

        EnsureFallbackTarget(caster);
        UpdateFallbackTarget(caster);

        _targetingPreview?.Begin(_currentSkill, caster, _currentPreview != null ? _currentPreview.transform : null);

        prepareResult = new SkillCastPrepareResult
        {
            AbilityIndex = index,
            Cost = Mathf.Max(0, _currentSkill.Cost),
            AnimatorAttackType = index + 1,
            CastAnimationStyle = _currentSkill.CastAnimationStyle
        };
        return true;
    }

    public bool TryGetPreparedSkill(out SkillDataSO skill)
    {
        skill = _currentSkill;
        return skill != null;
    }

    public void ExecutePreparedCast(IPlayableUnit caster)
    {
        if (_currentSkill == null || caster == null) return;

        UpdateFallbackTarget(caster);
        _targetingPreview?.End();

        SpawnCastVfx(caster);

        if (_currentSkill.CastType == SkillCastType.Area && _fallbackTarget != null)
            SkillCastVfxUtility.TrySpawnTransiient(_currentSkill.AreaEffectPrefab, _fallbackTarget.position, Quaternion.identity);
        else if (_currentSkill.CastType == SkillCastType.Self)
        {
            Vector3 p = SkillCastPresentationTarget.GetSelfCastFootWorld(caster, _currentSkill);
            Transform presentation = SkillCastPresentationTarget.GetSelfCastTransform(caster, _currentSkill);
            Quaternion rotation = presentation != null ? presentation.rotation : Quaternion.identity;
            SkillCastVfxUtility.TrySpawnTransiient(_currentSkill.SelfEffectPrefab, p, rotation);
        }

        Transform castTarget = _fallbackTarget != null ? _fallbackTarget : _currentPreview != null ? _currentPreview.transform : caster.UnitViewGO.transform;
        _currentSkill.OnCast(caster, castTarget);
        CleanupPreviewAndTarget();
        _currentSkill = null;
    }

    private void SpawnCastVfx(IPlayableUnit caster)
    {
        GameObject castPrefab = _currentSkill.GetCastPrefabForCurrentCastType();
        if (castPrefab == null) return;

        Vector3 position;
        Quaternion rotation;
        if (_currentSkill.CastType == SkillCastType.Self)
        {
            Transform skillSpot = caster.UnitSkillSpotTransform != null
                ? caster.UnitSkillSpotTransform
                : caster.UnitViewGO.transform;
            position = skillSpot.position;
            Transform presentation = SkillCastPresentationTarget.GetSelfCastTransform(caster, _currentSkill);
            rotation = presentation != null ? presentation.rotation : skillSpot.rotation;
        }
        else
        {
            position = NewSkillSystemSkillAimWorld.GetSkillOrigin(caster, caster);
            rotation = caster.UnitViewGO.transform.rotation;
        }

        SkillCastVfxUtility.TrySpawnTransiient(castPrefab, position, rotation);
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
            point = SkillCastPresentationTarget.GetSelfCastFootWorld(caster, _currentSkill);
        }
        else if (_currentSkill != null && _currentSkill.CastType == SkillCastType.Area)
        {
            point = NewSkillSystemSkillAimWorld.GetAreaClampedAimPoint(caster, caster, _currentSkill);
        }
        else if (_currentSkill != null && _currentSkill.CastType == SkillCastType.Projectile)
        {
            point = NewSkillSystemSkillAimWorld.GetPlanarClampedAimEnd(caster, caster, _currentSkill);
        }
        else
        {
            if (NewSkillSystemSkillAimWorld.TryMouseHitPoint(out Vector3 worldPoint))
                point = worldPoint;
            else
            {
                Vector3 planarFwd = Vector3.ProjectOnPlane(fallbackForward, Vector3.up);
                if (planarFwd.sqrMagnitude < 1e-8f) planarFwd = Vector3.forward;
                point = origin + planarFwd.normalized * 2f;
            }
            point = NewSkillSystemSkillAimWorld.ClampDirectedEnd(origin, point, _currentSkill != null ? _currentSkill.GetProjectileRange() : 500f);
        }

        if (_currentSkill == null || _currentSkill.CastType == SkillCastType.Self)
            CombatArenaBoundaryRuntime.TryClampVoluntaryWorldPosition(ref point);

        _fallbackTarget.position = point;
        Vector3 direction = point - origin;
        direction.y = 0f;
        if (direction.sqrMagnitude < 1e-6f) {
            direction = new Vector3(fallbackForward.x, 0f, fallbackForward.z);
        }
        if (direction.sqrMagnitude > 1e-6f)
            _fallbackTarget.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
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
