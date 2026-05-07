using UnityEngine;
using Logic.Scripts.GameDomain.Services.Skills;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/Cura")]
public class Cura : SkillDataSO
{
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
        caster.Heal(Power);
    }
}
