using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public static class SkillEffectsRunner
    {
        public static void Execute(
            SkillDataSO skill,
            IEffectable caster,
            UnityEngine.Transform target,
            IReadOnlyList<IEffectable> targets)
        {
            if (skill == null) return;
            SkillEffectSO[] effects = skill.Effects;
            if (effects == null || effects.Length == 0) return;

            SkillExecutionContext context = new SkillExecutionContext
            {
                Skill = skill,
                Caster = caster,
                TargetTransform = target,
                TargetPoint = target != null ? target.position : UnityEngine.Vector3.zero,
                Targets = targets
            };

            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffectSO effect = effects[i];
                if (effect == null) continue;
                effect.Execute(context);
            }
        }
    }
}
