using UnityEngine;
using UnityEngine.Serialization;
using Logic.Scripts.GameDomain.Services.Skills;
using System;
using System.Collections.Generic;

public abstract class SkillDataSO : ScriptableObject
{
    [Header("Divinity")]
    [SerializeField] private SkillDivinity _divinity = SkillDivinity.Hocari;

    [Header("Skill Type")]
    [SerializeField, FormerlySerializedAs("_skillTypeOverride")]
    private SkillType _skillType = SkillType.Damage;

    [Header("Cast Type")]
    [SerializeField, FormerlySerializedAs("_castModeOverride")]
    private SkillCastType _castType = SkillCastType.Projectile;

    [Header("Cast Animation")]
    [SerializeField] private SkillCastAnimationStyle _castAnimationStyle = SkillCastAnimationStyle.Slow;

    [Header("Projectile (when Cast Type is Projectile)")]
    [SerializeField] private float _projectileRange = 8f;
    [SerializeField] private int _projectileNumberOfTargets = 1;
    [SerializeField] private float _projectileTravelSpeed = 12f;
    [Tooltip("Mira: ground aim VFX at ability cast point; artist length along local +Y at scale 1; runtime lays flat and yaws to mouse, scale.y = projectile range.")]
    [SerializeField, FormerlySerializedAs("ProjectileAimPreviewPrefab")]
    private GameObject _projectileAimPrefab;
    [Tooltip("Cast: one-shot VFX on the caster when the skill is confirmed.")]
    [SerializeField] private GameObject _projectileCastPrefab;
    [Tooltip("Effect: traveling projectile (actual skill delivery).")]
    [SerializeField, FormerlySerializedAs("AttackPrefab"), FormerlySerializedAs("_projectilePrefab")]
    private GameObject _projectileEffectPrefab;
    [Tooltip("Impact: one-shot VFX spawned on an IEffectable when the projectile hits.")]
    [SerializeField] private GameObject _projectileImpactPrefab;

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
    [Tooltip("Mira: AoE aim ring/sphere at cast point.")]
    [SerializeField, FormerlySerializedAs("AoEPrefab")]
    private GameObject _areaAimPrefab;
    [Tooltip("Cast: one-shot VFX on the caster when the skill is confirmed.")]
    [SerializeField] private GameObject _areaCastPrefab;
    [Tooltip("Effect: AoE burst at the committed target point.")]
    [SerializeField, FormerlySerializedAs("_areaImpactPrefab")]
    private GameObject _areaEffectPrefab;

    [Header("Self (when Cast Type is Self)")]
    [Tooltip("Mira: self-buff aim anchor at feet during prepare.")]
    [SerializeField] private GameObject _selfAimPrefab;
    [Tooltip("Cast: one-shot VFX on the caster when the skill is confirmed.")]
    [SerializeField] private GameObject _selfCastPrefab;
    [Tooltip("Effect: buff/heal VFX at the presentation anchor (typically feet).")]
    [SerializeField, FormerlySerializedAs("_selfCastPrefab")]
    private GameObject _selfEffectPrefab;

    [Header("Skill")]
    public int Power, Cost;
    public Sprite Icon;
    public string SkillName;
    [TextArea(5, 18)]
    public string Description;
    [TextArea(5, 18)]
    public string Lore;

    [Header("Description — highlighted values")]
    [SerializeField] private SkillDescriptionHighlightEntry[] _descriptionHighlights = Array.Empty<SkillDescriptionHighlightEntry>();

    /// <summary>PlayerPrefs loadout identity: nome do ficheiro asset (<c>name</c> no Unity).</summary>
    public string LoadoutPersistenceKey => name;

    [Header("Effects Definition")]
    [SerializeField] private SkillEffectSO[] _effects = Array.Empty<SkillEffectSO>();

    [Header("Passive (Skill Type = Passive)")]
    [Tooltip("Applied once when the player starts a fight (Nara movement ring + mana gained per turn).")]
    [SerializeField] private PassiveStatModifierEntry[] _passiveModifiers = Array.Empty<PassiveStatModifierEntry>();
    [Tooltip("Optional per-turn passive logic (e.g. random roulette buff at each player turn start).")]
    [SerializeField] private PassiveTurnBehaviorSO _passiveTurnBehavior;
    [Tooltip("Optional fight-long passive logic (e.g. low-HP outgoing damage scaling).")]
    [SerializeField] private PassiveCombatBehaviorSO _passiveCombatBehavior;
    [Tooltip("Optional passive logic when the player takes damage (e.g. stack movement bonus next turn).")]
    [SerializeField] private PassiveOnDamageTakenBehaviorSO _passiveOnDamageTakenBehavior;

    public SkillDivinity Divinity => _divinity;
    public SkillType SkillType => _skillType;

    /// <summary>Passive and SelfBuff skills always behave as <see cref="SkillCastType.Self"/> at runtime.</summary>
    private SkillCastType EffectiveCastType =>
        _skillType == SkillType.Passive || _skillType == SkillType.SelfBuff
            ? SkillCastType.Self
            : _castType;

    public SkillCastType CastType => EffectiveCastType;

    public SkillCastAnimationStyle CastAnimationStyle =>
        EffectiveCastType == SkillCastType.Self && SkillType == SkillType.SelfBuff
            ? SkillCastAnimationStyle.Fast
            : _castAnimationStyle;
    public SkillEffectSO[] Effects => _effects;

    public SkillDescriptionHighlightEntry[] DescriptionHighlights =>
        _descriptionHighlights ?? Array.Empty<SkillDescriptionHighlightEntry>();

    /// <summary>Entries for <see cref="SkillType.Passive"/>; ignored for other skill types at runtime.</summary>
    public PassiveStatModifierEntry[] PassiveModifiers => _passiveModifiers ?? Array.Empty<PassiveStatModifierEntry>();

    /// <summary>Per-turn passive behavior for <see cref="SkillType.Passive"/> (e.g. roulette).</summary>
    public PassiveTurnBehaviorSO PassiveTurnBehavior => _passiveTurnBehavior;

    /// <summary>Fight-long passive behavior for <see cref="SkillType.Passive"/> (e.g. low-HP damage scaling).</summary>
    public PassiveCombatBehaviorSO PassiveCombatBehavior => _passiveCombatBehavior;

    /// <summary>Passive behavior triggered when the player takes damage.</summary>
    public PassiveOnDamageTakenBehaviorSO PassiveOnDamageTakenBehavior => _passiveOnDamageTakenBehavior;

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
    public GameObject ProjectileCastPrefab => _projectileCastPrefab;
    public GameObject ProjectileEffectPrefab => _projectileEffectPrefab;
    public GameObject ProjectileImpactPrefab => _projectileImpactPrefab;
    /// <summary>Backward-compatible alias for <see cref="ProjectileEffectPrefab"/>.</summary>
    public GameObject ProjectilePrefab => ProjectileEffectPrefab;

    public GameObject AreaAimPrefab => _areaAimPrefab;
    public GameObject AreaCastPrefab => _areaCastPrefab;
    public GameObject AreaEffectPrefab => _areaEffectPrefab;
    /// <summary>Backward-compatible alias for <see cref="AreaEffectPrefab"/>.</summary>
    public GameObject AreaImpactPrefab => AreaEffectPrefab;

    public GameObject SelfAimPrefab => _selfAimPrefab;
    public GameObject SelfCastPrefab => _selfCastPrefab;
    public GameObject SelfEffectPrefab => _selfEffectPrefab;

    public GameObject GetCastPrefabForCurrentCastType()
    {
        switch (EffectiveCastType)
        {
            case SkillCastType.Projectile: return _projectileCastPrefab;
            case SkillCastType.Area: return _areaCastPrefab;
            case SkillCastType.Self: return _selfCastPrefab;
            default: return null;
        }
    }

    public GameObject GetEffectPrefabForCurrentCastType()
    {
        switch (EffectiveCastType)
        {
            case SkillCastType.Projectile: return _projectileEffectPrefab;
            case SkillCastType.Area: return _areaEffectPrefab;
            case SkillCastType.Self: return _selfEffectPrefab;
            default: return null;
        }
    }

    protected virtual IReadOnlyList<IEffectable> ResolveTargets(IEffectable caster, Transform target, IEffectable beneficiary = null)
    {
        if (EffectiveCastType == SkillCastType.Self)
        {
            IEffectable selfTarget = SkillCastRules.IsRangelessSelfBuff(this)
                ? beneficiary ?? SkillCastBeneficiaryResolver.TryResolve(this, caster)
                : caster;
            if (selfTarget == null) return Array.Empty<IEffectable>();
            return new[] { selfTarget };
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
        IEffectable beneficiary = SkillCastBeneficiaryResolver.TryResolve(this, caster);
        OutgoingDamageLifestealRuntime.ClearForCaster(beneficiary);
        IReadOnlyList<IEffectable> targets = ResolveTargets(caster, target, beneficiary);
        SkillEffectsRunner.Execute(this, caster, target, targets, beneficiary);
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
        if (_skillType == SkillType.SelfBuff && _castType != SkillCastType.Self)
        {
            Debug.LogWarning($"[SkillDataSO] '{name}' is SelfBuff but Cast Type is {_castType}; forcing Self at runtime via EffectiveCastType is not applied — set Cast Type to Self in the asset.", this);
            _castType = SkillCastType.Self;
        }
    }
}
