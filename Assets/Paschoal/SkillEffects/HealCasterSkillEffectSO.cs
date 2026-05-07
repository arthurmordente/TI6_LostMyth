using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(fileName = "HealCasterEffect", menuName = "ScriptableObjects/Skills/Effects/HealCaster")]
public class HealCasterSkillEffectSO : SkillEffectSO
{
    [SerializeField] private int _flatHealBonus;

    public override void Execute(in SkillExecutionContext context)
    {
        if (context.Caster == null) return;
        int amount = Mathf.Max(0, context.Skill.Power + _flatHealBonus);
        context.Caster.Heal(amount);
        context.Caster.PreviewHeal(amount);
    }
}
