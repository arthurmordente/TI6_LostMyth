using UnityEngine;
using Logic.Scripts.GameDomain.Services.Skills;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/CuraPorTurno")]
public class CuraPorTurno : SkillDataSO
{
    public EffectSO effectSO;

    protected override SkillType GetDefaultSkillType()
    {
        return SkillType.SelfBuff;
    }

    protected override SkillCastMode GetDefaultCastMode()
    {
        return SkillCastMode.Self;
    }

    public override void OnCast(IEffectable caster, Transform target)
    {
        //caster.AddEffect(effectSO);
    }
}
