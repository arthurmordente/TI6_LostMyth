using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Estimates how much HP a self-buff heal would restore (matches <see cref="HealCasterSkillEffectSO"/> stacking).
    /// </summary>
    public static class SkillCastSelfHealPreview
    {
        public static bool TryGetSelfHealPreviewAmount(SkillDataSO skill, out int totalHeal)
        {
            totalHeal = 0;
            if (skill == null) return false;
            if (skill.CastType != SkillCastType.Self || skill.SkillType != SkillType.SelfBuff) return false;

            SkillEffectSO[] effects = skill.Effects;
            if (effects == null || effects.Length == 0) return false;

            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] is HealCasterSkillEffectSO h)
                    totalHeal += h.ComputeTotalHealForSkillPower(skill.Power);
            }

            return totalHeal > 0;
        }
    }
}
