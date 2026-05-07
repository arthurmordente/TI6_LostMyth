using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageTargetsEffect", menuName = "ScriptableObjects/Skills/Effects/DamageTargets")]
public class DamageTargetsSkillEffectSO : SkillEffectSO
{
    [SerializeField] private int _flatDamageBonus;

    public override void Execute(in SkillExecutionContext context)
    {
        if (context.Targets == null) return;
        int amount = Mathf.Max(0, context.Skill.Power + _flatDamageBonus);
        for (int i = 0; i < context.Targets.Count; i++)
        {
            IEffectable target = context.Targets[i];
            if (target == null) continue;
            target.TakeDamage(amount);
            target.PreviewDamage(amount);
        }
    }
}
