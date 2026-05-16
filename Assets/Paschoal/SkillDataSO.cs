using UnityEngine;
using UnityEngine.Serialization;
using Logic.Scripts.GameDomain.Services.Skills;
using System;
using System.Collections.Generic;

public abstract class SkillDataSO : ScriptableObject
{
    [Header("Skill Type")]
    [SerializeField, FormerlySerializedAs("_skillTypeOverride")]
    private SkillType _skillType = SkillType.Damage;

    [Header("Cast Type")]
    [SerializeField, FormerlySerializedAs("_castModeOverride")]
    private SkillCastType _castType = SkillCastType.Projectile;

    [Header("Projectile (when Cast Type is Projectile)")]
    [SerializeField] private float _projectileRange = 8f;
    [SerializeField] private int _projectileNumberOfTargets = 1;
    [SerializeField] private float _projectileTravelSpeed = 12f;
    [Tooltip("Ground aim VFX at ability cast point: artist length along local +Y at scale 1; runtime lays flat and yaws to mouse, scale.y = projectile range.")]
    [SerializeField, FormerlySerializedAs("ProjectileAimPreviewPrefab")]
    private GameObject _projectileAimPrefab;
    [SerializeField, FormerlySerializedAs("AttackPrefab")]
    private GameObject _projectilePrefab;

    [Header("Projectile movement / pull to target (Projectile only)")]
    [Tooltip("When enabled, hitting another IEffectable pulls the caster next to that target (miss = no movement). Uses the same hit rules as projectile damage.")]
    [SerializeField] private bool _moveCasterToProjectileHit;
    [Tooltip("Horizontal distance from the enemy root where the caster stops when pulled (avoid overlap).")]
    [SerializeField, FormerlySerializedAs("_projectileHitSurfaceSeparation")]
    private float _projectilePullStandoffFromTargetMeters = 1.2f;
    [Tooltip("When false, projectile hits apply no HP damage (still counts as a hit for Max Targets).")]
    [SerializeField] private bool _projectileDealsDamage = true;
    [Tooltip("When true, arena radius/recenter after cast waits until the projectile resolves movement (or hits nothing).")]
    [SerializeField] private bool _projectileDefersArenaSyncUntilHit;

    [SerializeField] private float _projectileSpawnForwardOffset = 0.35f;
    [Tooltip("Ignore hits until the projectile has travelled this far from spawn (avoids instant self-hits).")]
    [SerializeField] private float _projectileMinTravelBeforeHitMeters = 0.45f;
    [SerializeField] private float _projectileHitDisplacementDurationSeconds = 0.35f;

    [Header("Area (when Cast Type is Area)")]
    [SerializeField] private float _areaMinRange;
    [SerializeField] private float _areaMaxRange = 8f;
    [SerializeField] private float _areaRadius = 2f;
    [SerializeField, FormerlySerializedAs("AoEPrefab")]
    private GameObject _areaAimPrefab;
    [SerializeField] private GameObject _areaImpactPrefab;

    [Header("Self (when Cast Type is Self)")]
    [SerializeField] private GameObject _selfAimPrefab;
    [SerializeField] private GameObject _selfCastPrefab;

    [Header("Skill")]
    public int Power, Cost;
    public Sprite Icon;
    public string SkillName;
    [TextArea(5, 18)]
    public string Description;

    /// <summary>PlayerPrefs loadout identity: nome do ficheiro asset (<c>name</c> no Unity).</summary>
    public string LoadoutPersistenceKey => name;

    [Header("Effects Definition")]
    [SerializeField] private SkillEffectSO[] _effects = Array.Empty<SkillEffectSO>();

    [Header("Passive (Skill Type = Passive)")]
    [Tooltip("Applied once when the player starts a fight (Nara movement ring + mana gained per turn).")]
    [SerializeField] private PassiveStatModifierEntry[] _passiveModifiers = Array.Empty<PassiveStatModifierEntry>();

    public SkillType SkillType => _skillType;

    /// <summary>Passive skills always behave as <see cref="SkillCastType.Self"/> at runtime (serialized cast is forced in <see cref="OnValidate"/>).</summary>
    private SkillCastType EffectiveCastType =>
        _skillType == SkillType.Passive ? SkillCastType.Self : _castType;

    public SkillCastType CastType => EffectiveCastType;
    public SkillEffectSO[] Effects => _effects;

    /// <summary>Entries for <see cref="SkillType.Passive"/>; ignored for other skill types at runtime.</summary>
    public PassiveStatModifierEntry[] PassiveModifiers => _passiveModifiers ?? Array.Empty<PassiveStatModifierEntry>();

    public bool IsCastable => SkillType != SkillType.Passive;

    /// <summary>Derived display range for UI / legacy helpers (Projectile: range; Area: max ring; Self: 0).</summary>
    public float Range
    {
        get
        {
            switch (EffectiveCastType)
            {
                case SkillCastType.Projectile:
                    return GetProjectileRange();
                case SkillCastType.Area:
                    return GetAreaMaxCastDistance();
                default:
                    return 0f;
            }
        }
    }

    /// <summary>No cooldown data on this asset yet; kept for callers that read <see cref="CoolDown"/>.</summary>
    public float CoolDown => 0f;

    public GameObject ProjectileAimPrefab => _projectileAimPrefab;
    public GameObject ProjectilePrefab => _projectilePrefab;
    public GameObject AreaAimPrefab => _areaAimPrefab;
    public GameObject AreaImpactPrefab => _areaImpactPrefab;
    public GameObject SelfAimPrefab => _selfAimPrefab;
    public GameObject SelfCastPrefab => _selfCastPrefab;

    protected virtual IReadOnlyList<IEffectable> ResolveTargets(IEffectable caster, Transform target)
    {
        if (EffectiveCastType == SkillCastType.Self)
        {
            if (caster == null) return Array.Empty<IEffectable>();
            return new[] { caster };
        }

        Vector3 center = target != null ? target.position : (caster != null && caster.GetReferenceTransform() != null ? caster.GetReferenceTransform().position : Vector3.zero);
        float radius = GetAreaRadius();
        if (EffectiveCastType == SkillCastType.Area && radius > 0.0001f)
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
        if (EffectiveCastType != SkillCastType.Projectile) return 0f;
        return _projectileRange > 0.0001f ? _projectileRange : 500f;
    }

    public int GetProjectileMaxTargets()
    {
        if (EffectiveCastType != SkillCastType.Projectile) return 1;
        return Mathf.Max(1, _projectileNumberOfTargets);
    }

    public float GetProjectileSpeed()
    {
        if (EffectiveCastType != SkillCastType.Projectile) return 12f;
        return _projectileTravelSpeed > 0.0001f ? _projectileTravelSpeed : 12f;
    }

    /// <summary>Pull caster toward hit <see cref="IEffectable"/> when the movement projectile option is enabled.</summary>
    public bool MoveCasterToProjectileHit =>
        EffectiveCastType == SkillCastType.Projectile && _moveCasterToProjectileHit;

    public float ProjectilePullStandoffFromTargetMeters =>
        Mathf.Max(0.1f, _projectilePullStandoffFromTargetMeters);

    public int GetProjectileCollisionDamage()
    {
        if (EffectiveCastType != SkillCastType.Projectile || !_projectileDealsDamage) return 0;
        return Power;
    }

    public bool ShouldDeferArenaSyncUntilProjectileHit() =>
        EffectiveCastType == SkillCastType.Projectile
        && (_projectileDefersArenaSyncUntilHit || _moveCasterToProjectileHit);

    public float ProjectileSpawnForwardOffset => Mathf.Max(0f, _projectileSpawnForwardOffset);

    public float ProjectileMinTravelBeforeHitMeters => Mathf.Max(0f, _projectileMinTravelBeforeHitMeters);

    public float ProjectileHitDisplacementDurationSeconds =>
        Mathf.Max(0.05f, _projectileHitDisplacementDurationSeconds);

    public float GetAreaRadius()
    {
        if (EffectiveCastType != SkillCastType.Area) return 0f;
        return _areaRadius > 0.0001f ? _areaRadius : 0f;
    }

    public float GetAreaMinCastDistance()
    {
        if (EffectiveCastType != SkillCastType.Area) return 0f;
        return Mathf.Max(0f, _areaMinRange);
    }

    public float GetAreaMaxCastDistance()
    {
        if (EffectiveCastType != SkillCastType.Area) return 0f;
        float min = GetAreaMinCastDistance();
        if (_areaMaxRange <= 0.0001f && min <= 0.0001f)
            return 0f;
        float max = _areaMaxRange > 0.0001f ? _areaMaxRange : 0f;
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
        if (_castType == SkillCastType.Area && _areaMaxRange < _areaMinRange)
            _areaMaxRange = _areaMinRange;
        if (_projectileNumberOfTargets < 1)
            _projectileNumberOfTargets = 1;
        if (_projectileTravelSpeed <= 0f)
            _projectileTravelSpeed = 12f;
        if (_projectilePullStandoffFromTargetMeters < 0.1f)
            _projectilePullStandoffFromTargetMeters = 0.1f;
        if (_projectileSpawnForwardOffset < 0f)
            _projectileSpawnForwardOffset = 0f;
        if (_projectileMinTravelBeforeHitMeters < 0f)
            _projectileMinTravelBeforeHitMeters = 0f;
        if (_projectileHitDisplacementDurationSeconds < 0.05f)
            _projectileHitDisplacementDurationSeconds = 0.05f;
        if (_castType == SkillCastType.Projectile && _skillType != SkillType.Movement && _moveCasterToProjectileHit)
            _moveCasterToProjectileHit = false;
        if (_skillType == SkillType.Passive) {
            Cost = 0;
            _castType = SkillCastType.Self;
        }
    }
}
