using UnityEngine;
using Logic.Scripts.GameDomain.Services.Skills;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/Shield")]
public class Shield : SkillDataSO
{
    public EffectSO effectSO;

    protected override SkillType GetDefaultSkillType()
    {
        return SkillType.SelfBuff;
    }

    public override void OnCast(IEffectable caster = null, Transform target = null)
    {
        //caster.AddEffect(effectSO)
    }
}
