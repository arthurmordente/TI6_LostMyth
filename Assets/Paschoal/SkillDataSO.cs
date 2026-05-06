using UnityEngine;
using Logic.Scripts.GameDomain.Services.Skills;

public abstract class SkillDataSO : ScriptableObject
{
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
    //public RuntimeAnimatorController animationOverride;
    //public string AnimationID;

    public SkillType SkillType => _useSkillTypeOverride ? _skillTypeOverride : GetDefaultSkillType();

    protected virtual SkillType GetDefaultSkillType()
    {
        return SkillType.Damage;
    }

    public abstract void OnCast(IEffectable caster = null, Transform target = null);
}
