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
    [SerializeField, FormerlySerializedAs("ProjectileAimPreviewPrefab")]
    private GameObject _projectileAimPrefab;
    [SerializeField, FormerlySerializedAs("AttackPrefab")]
    private GameObject _projectilePrefab;

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
    public string SkillName, Description;

    [Header("Loadout persistence")]
    [Tooltip("Chave única para guardar o slot do loadout entre Exploração e Luta (PlayerPrefs). Se vazio, usa o nome do asset Unity (ficheiro). Use IDs distintos se vários skills tiverem o mesmo nome de ficheiro.")]
    [SerializeField] private string _loadoutPersistenceKey;

    /// <summary>Chave usada no serviço de loadout (independente da ordem do array do catálogo entre cenas).</summary>
    public string LoadoutPersistenceKey => string.IsNullOrWhiteSpace(_loadoutPersistenceKey) ? name : _loadoutPersistenceKey.Trim();

    [Header("Effects Definition")]
    [SerializeField] private SkillEffectSO[] _effects = Array.Empty<SkillEffectSO>();

    public SkillType SkillType => _skillType;
    public SkillCastType CastType => _castType;
    public SkillEffectSO[] Effects => _effects;
    public bool IsCastable => SkillType != SkillType.Passive;

    /// <summary>Derived display range for UI / legacy helpers (Projectile: range; Area: max ring; Self: 0).</summary>
    public float Range
    {
        get
        {
            switch (_castType)
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
        if (_castType == SkillCastType.Self)
        {
            if (caster == null) return Array.Empty<IEffectable>();
            return new[] { caster };
        }

        Vector3 center = target != null ? target.position : (caster != null && caster.GetReferenceTransform() != null ? caster.GetReferenceTransform().position : Vector3.zero);
        float radius = GetAreaRadius();
        if (_castType == SkillCastType.Area && radius > 0.0001f)
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
        if (_castType != SkillCastType.Projectile) return 0f;
        return _projectileRange > 0.0001f ? _projectileRange : 500f;
    }

    public int GetProjectileMaxTargets()
    {
        if (_castType != SkillCastType.Projectile) return 1;
        return Mathf.Max(1, _projectileNumberOfTargets);
    }

    public float GetProjectileSpeed()
    {
        if (_castType != SkillCastType.Projectile) return 12f;
        return _projectileTravelSpeed > 0.0001f ? _projectileTravelSpeed : 12f;
    }

    public float GetAreaRadius()
    {
        if (_castType != SkillCastType.Area) return 0f;
        return _areaRadius > 0.0001f ? _areaRadius : 0f;
    }

    public float GetAreaMinCastDistance()
    {
        if (_castType != SkillCastType.Area) return 0f;
        return Mathf.Max(0f, _areaMinRange);
    }

    public float GetAreaMaxCastDistance()
    {
        if (_castType != SkillCastType.Area) return 0f;
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
        if (SkillType == SkillType.Passive)
            Cost = 0;
    }
}
