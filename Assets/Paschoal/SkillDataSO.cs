using UnityEngine;
using Logic.Scripts.GameDomain.Services.Skills;
using System;
using System.Collections.Generic;

public abstract class SkillDataSO : ScriptableObject
{
    public enum ProjectileHitMode
    {
        StopOnFirstTarget = 0,
        PierceUpToMaxTargets = 1
    }

    [Serializable]
    public struct ProjectileCastData
    {
        public float Range;
        public int MaxTargets;
        [Tooltip("Stop at first enemy hit, or pierce until MaxTargets hits.")]
        public ProjectileHitMode HitMode;
    }

    [Serializable]
    public struct AreaCastData
    {
        public float MinCastDistance;
        public float MaxCastDistance;
        public float Radius;
    }

    [Space(10)]
    [Header("SkillData Properties")]

    public float CoolDown, CastTime, Range, AreaOfEffect;
    /// <summary>Horizontal radius of <see cref="AoEPrefab"/> at localScale 1. Preview scale = AreaOfEffect / this so decal matches gameplay radius.</summary>
    public float AoEPrefabBaseRadius = 1f;
    public int Power, Cost;
    public Sprite Icon;
    public string SkillName, Description;
    public GameObject AoEPrefab, AttackPrefab;
    public SkillDataSO Upgrade;
    [Header("Skill Type")]
    [Tooltip("When enabled, this override forces the skill type for this asset. Keep disabled to use script defaults.")]
    [SerializeField] private bool _useSkillTypeOverride;
    [SerializeField] private SkillType _skillTypeOverride = SkillType.Damage;
    [Header("Cast Definition")]
    [SerializeField] private bool _useCastModeOverride;
    [SerializeField] private SkillCastMode _castModeOverride = SkillCastMode.Projectile;
    [SerializeField] private ProjectileCastData _projectileCast = new ProjectileCastData { Range = 8f, MaxTargets = 1, HitMode = ProjectileHitMode.StopOnFirstTarget };
    [SerializeField] private AreaCastData _areaCast = new AreaCastData { MinCastDistance = 0f, MaxCastDistance = 8f, Radius = 2f };
    [SerializeField] private bool _useSelfCastOnCaster = true;
    [Header("Effects Definition")]
    [SerializeField] private SkillEffectSO[] _effects = Array.Empty<SkillEffectSO>();
    //public RuntimeAnimatorController animationOverride;
    //public string AnimationID;

    public SkillType SkillType => _useSkillTypeOverride ? _skillTypeOverride : GetDefaultSkillType();
    public SkillCastMode CastMode => _useCastModeOverride ? _castModeOverride : GetDefaultCastMode();
    public ProjectileCastData ProjectileCast => _projectileCast;
    public AreaCastData AreaCast => _areaCast;
    public bool UseSelfCastOnCaster => _useSelfCastOnCaster;
    public SkillEffectSO[] Effects => _effects;
    public bool IsCastable => SkillType != SkillType.Passive;

    protected virtual SkillType GetDefaultSkillType()
    {
        return SkillType.Damage;
    }

    protected virtual SkillCastMode GetDefaultCastMode()
    {
        return SkillCastMode.Projectile;
    }

    protected virtual IReadOnlyList<IEffectable> ResolveTargets(IEffectable caster, Transform target)
    {
        if (CastMode == SkillCastMode.Self)
        {
            if (caster == null) return Array.Empty<IEffectable>();
            return new[] { caster };
        }

        Vector3 center = target != null ? target.position : (caster != null && caster.GetReferenceTransform() != null ? caster.GetReferenceTransform().position : Vector3.zero);
        float radius = GetAreaRadius();
        if (CastMode == SkillCastMode.Area && radius > 0.0001f)
        {
            var hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
            List<IEffectable> resolved = new List<IEffectable>(hits.Length);
            for (int i = 0; i < hits.Length; i++)
            {
                var effectable = hits[i].GetComponentInParent<IEffectable>();
                if (effectable == null) continue;
                if (ReferenceEquals(effectable, caster)) continue;
                if (!resolved.Contains(effectable))
                    resolved.Add(effectable);
            }
            return resolved;
        }

        if (target != null)
        {
            var directTarget = target.GetComponentInParent<IEffectable>();
            if (directTarget != null)
                return new[] { directTarget };
        }
        return Array.Empty<IEffectable>();
    }

    public float GetProjectileRange()
    {
        if (_projectileCast.Range > 0.0001f) return _projectileCast.Range;
        return Range > 0.0001f ? Range : 500f;
    }

    public int GetProjectileMaxTargets()
    {
        return Mathf.Max(1, _projectileCast.MaxTargets);
    }

    public ProjectileHitMode GetProjectileHitMode()
    {
        return _projectileCast.HitMode;
    }

    public float GetAreaRadius()
    {
        if (_areaCast.Radius > 0.0001f) return _areaCast.Radius;
        return AreaOfEffect > 0.0001f ? AreaOfEffect : 0f;
    }

    public float GetAreaMinCastDistance()
    {
        return Mathf.Max(0f, _areaCast.MinCastDistance);
    }

    public float GetAreaMaxCastDistance()
    {
        float min = GetAreaMinCastDistance();
        if (_areaCast.MaxCastDistance <= 0.0001f && min <= 0.0001f)
            return 0f;
        float max = _areaCast.MaxCastDistance > 0.0001f ? _areaCast.MaxCastDistance : Range;
        return Mathf.Max(min, max);
    }

    public virtual void OnCast(IEffectable caster = null, Transform target = null)
    {
        if (!IsCastable) return;
        IReadOnlyList<IEffectable> targets = ResolveTargets(caster, target);
        SkillEffectsRunner.Execute(this, caster, target, targets);
    }

    private void OnValidate()
    {
        if (CastMode == SkillCastMode.Area && _areaCast.MaxCastDistance < _areaCast.MinCastDistance)
            _areaCast.MaxCastDistance = _areaCast.MinCastDistance;
        if (_projectileCast.MaxTargets < 1)
            _projectileCast.MaxTargets = 1;
        if (CastMode == SkillCastMode.Self || SkillType == SkillType.Passive)
            Cost = 0;
    }
}
