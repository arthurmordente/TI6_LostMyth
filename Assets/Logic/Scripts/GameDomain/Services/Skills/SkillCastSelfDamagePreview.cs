using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// HP preview while aiming a skill that uses <see cref="DamageCasterSkillEffectSO"/>.
    /// Shield absorption uses the same rules as <see cref="IEffectable.PreviewDamage"/> on the caster.
    /// </summary>
    public static class SkillCastSelfDamagePreview
    {
        public static bool TryGetSelfDamagePreviewAmount(SkillDataSO skill, out int totalDamage)
        {
            totalDamage = 0;
            if (skill == null) return false;

            SkillEffectSO[] effects = skill.Effects;
            if (effects == null || effects.Length == 0) return false;

            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] is DamageCasterSkillEffectSO d)
                    totalDamage += d.EffectDamageAmount;
            }

            return totalDamage > 0;
        }
    }
}
