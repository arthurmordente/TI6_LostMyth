using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public static class SkillEffectsRunner
    {
        public static void Execute(
            SkillDataSO skill,
            IEffectable caster,
            UnityEngine.Transform target,
            IReadOnlyList<IEffectable> targets,
            IEffectable beneficiary = null)
        {
            if (skill == null) return;
            SkillEffectSO[] effects = skill.Effects;
            if (effects == null || effects.Length == 0) return;

            IEffectable resolvedBeneficiary = beneficiary ?? SkillCastBeneficiaryResolver.TryResolve(skill, caster);

            SkillExecutionContext context = new SkillExecutionContext
            {
                Skill = skill,
                Caster = caster,
                Beneficiary = resolvedBeneficiary,
                TargetTransform = target,
                TargetPoint = target != null ? target.position : UnityEngine.Vector3.zero,
                Targets = targets
            };

            bool declarativeProjectileSpawnHandled = skill is DeclarativeSkillDataSO
                && skill.CastType == SkillCastType.Projectile
                && skill.ProjectilePrefab != null;

            for (int i = 0; i < effects.Length; i++)
            {
                SkillEffectSO effect = effects[i];
                if (effect == null) continue;
                if (declarativeProjectileSpawnHandled && effect is ISpawnProjectileSkillEffect)
                    continue;
                effect.Execute(context);
            }
        }
    }
}
