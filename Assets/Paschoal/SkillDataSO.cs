using UnityEngine;

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
    //public RuntimeAnimatorController animationOverride;
    //public string AnimationID;

    public abstract void OnCast(IEffectable caster = null, Transform target = null);
}
