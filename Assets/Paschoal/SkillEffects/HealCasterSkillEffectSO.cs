using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(fileName = "HealCasterEffect", menuName = "ScriptableObjects/Skills/Effects/HealCaster")]
public class HealCasterSkillEffectSO : SkillEffectSO
{
    [SerializeField] private int _flatHealBonus;

    public int ComputeTotalHealForSkillPower(int skillPower) => Mathf.Max(0, skillPower + _flatHealBonus);

    public override void Execute(in SkillExecutionContext context)
    {
        if (context.Caster == null) return;
        int amount = ComputeTotalHealForSkillPower(context.Skill.Power);
        context.Caster.Heal(amount);
    }
}
