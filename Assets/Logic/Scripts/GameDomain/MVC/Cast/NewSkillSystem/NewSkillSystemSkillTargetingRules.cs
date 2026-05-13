using UnityEngine;
using Logic.Scripts.GameDomain.Services.Skills;

namespace Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem {
    /// <summary>
    /// Derives how new skill system aim preview should resolve targets from <see cref="SkillDataSO"/> data.
    /// </summary>
    public static class NewSkillSystemSkillTargetingRules {
        public static NewSkillSystemAimHighlightKind GetHighlightKind(SkillDataSO skill) {
            if (skill == null) return NewSkillSystemAimHighlightKind.None;
            if (skill.CastType == SkillCastType.Self)
                return skill.SelfAimPrefab != null ? NewSkillSystemAimHighlightKind.SelfFootAnchor : NewSkillSystemAimHighlightKind.None;
            if (skill.CastType == SkillCastType.Area) return NewSkillSystemAimHighlightKind.GroundAreaSphere;
            if (skill.CastType == SkillCastType.Projectile) return NewSkillSystemAimHighlightKind.DirectedLine;
            return NewSkillSystemAimHighlightKind.None;
        }

        /// <summary>
        /// When true, multiple <see cref="IEffectable"/> along the aim segment may be highlighted (N targets &gt; 1).
        /// </summary>
        public static bool GetDirectedLineUsesPierce(SkillDataSO skill) {
            if (skill == null) return false;
            return skill.GetProjectileMaxTargets() > 1;
        }

        public static int GetDirectedLineMaxTargets(SkillDataSO skill)
        {
            if (skill == null) return 1;
            return skill.GetProjectileMaxTargets();
        }
    }
}
