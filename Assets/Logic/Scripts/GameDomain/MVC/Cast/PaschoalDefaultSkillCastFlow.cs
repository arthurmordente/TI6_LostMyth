using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

public class PaschoalDefaultSkillCastFlow : ISkillCastFlow
{
    private SkillDataSO _currentSkill;
    private GameObject _currentPreview;
    private Transform _fallbackTarget;

    public bool CanHandleCaster(IPlayableUnit caster)
    {
        if (caster == null || caster.UnitViewGO == null) return false;

        var legacyToggle = caster.UnitViewGO.GetComponent<LegacySkillSystemToggle>();
        if (legacyToggle != null && legacyToggle.UseLegacySkillSystem) return false;

        var loadout = caster.UnitViewGO.GetComponent<PaschoalSkillLoadout>();
        return loadout != null;
    }

    public void InitEntryPoint(INaraController naraController)
    {
        // No setup required for Paschoal's ScriptableObject casts.
    }

    public bool TryPrepareCast(int index, IPlayableUnit caster, out SkillCastPrepareResult prepareResult)
    {
        prepareResult = default;
        if (caster == null || caster.UnitViewGO == null) return false;

        var legacyToggle = caster.UnitViewGO.GetComponent<LegacySkillSystemToggle>();
        if (legacyToggle != null && legacyToggle.UseLegacySkillSystem) return false;

        var loadout = caster.UnitViewGO.GetComponent<PaschoalSkillLoadout>();
        if (loadout == null) return false;
        if (!loadout.TryGetSkill(index, out SkillDataSO skill)) return false;

        _currentSkill = skill;
        if (_currentSkill.AoEPrefab != null)
        {
            Transform spawn = caster.UnitSkillSpotTransform != null ? caster.UnitSkillSpotTransform : caster.UnitViewGO.transform;
            _currentPreview = Object.Instantiate(_currentSkill.AoEPrefab, spawn.position, spawn.rotation);
        }

        EnsureFallbackTarget(caster);
        UpdateFallbackTarget(caster);

        prepareResult = new SkillCastPrepareResult
        {
            AbilityIndex = index,
            Cost = Mathf.Max(0, _currentSkill.Cost),
            AnimatorAttackType = index + 1
        };
        return true;
    }

    public void ExecutePreparedCast(IPlayableUnit caster)
    {
        if (_currentSkill == null || caster == null) return;

        UpdateFallbackTarget(caster);
        Transform castTarget = _currentPreview != null ? _currentPreview.transform : _fallbackTarget;
        _currentSkill.OnCast(caster, castTarget);
        CleanupPreviewAndTarget();
        _currentSkill = null;
    }

    public void CancelPreparedCast(IPlayableUnit caster)
    {
        CleanupPreviewAndTarget();
        _currentSkill = null;
    }

    private void EnsureFallbackTarget(IPlayableUnit caster)
    {
        if (_fallbackTarget != null) return;
        var go = new GameObject("PaschoalCastTarget");
        _fallbackTarget = go.transform;
        if (caster?.UnitViewGO != null)
            _fallbackTarget.position = caster.UnitViewGO.transform.position;
    }

    private void UpdateFallbackTarget(IPlayableUnit caster)
    {
        if (_fallbackTarget == null || caster == null || caster.UnitViewGO == null) return;

        Vector3 origin = caster.UnitSkillSpotTransform != null ? caster.UnitSkillSpotTransform.position : caster.UnitViewGO.transform.position;
        Vector3 fallbackForward = caster.UnitViewGO.transform.forward;
        Vector3 point = TryGetMouseWorldPoint(out Vector3 worldPoint) ? worldPoint : (origin + fallbackForward * 2f);

        _fallbackTarget.position = point;
        Vector3 direction = point - origin;
        if (direction.sqrMagnitude > 0.0001f)
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
