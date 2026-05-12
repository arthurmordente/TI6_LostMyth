using UnityEngine;
using Logic.Scripts.GameDomain.Services.Skills;

namespace Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem {
    /// <summary>
    /// Derives how new skill system aim preview should resolve targets from <see cref="SkillDataSO"/> data,
    /// without per-asset inspector overrides (zero serialized changes on existing skills).
    /// </summary>
    public static class NewSkillSystemSkillTargetingRules {
        public static NewSkillSystemAimHighlightKind GetHighlightKind(SkillDataSO skill) {
            if (skill == null) return NewSkillSystemAimHighlightKind.None;
            if (skill.CastMode == SkillCastMode.Self) return NewSkillSystemAimHighlightKind.None;
            if (skill.CastMode == SkillCastMode.Area) return NewSkillSystemAimHighlightKind.GroundAreaSphere;
            if (skill.CastMode == SkillCastMode.Projectile) return NewSkillSystemAimHighlightKind.DirectedLine;
            return NewSkillSystemAimHighlightKind.None;
        }

        /// <summary>
        /// When true, every non-player <see cref="IEffectable"/> along the aim segment may be highlighted.
        /// When false, only the first such target along the ray is highlighted (stops at first hittable).
        /// Driven only by <see cref="SkillDataSO.GetProjectileHitMode"/> on the skill asset (no prefab scripts).
        /// </summary>
        public static bool GetDirectedLineUsesPierce(SkillDataSO skill) {
            if (skill == null) return false;
            return skill.GetProjectileHitMode() == SkillDataSO.ProjectileHitMode.PierceUpToMaxTargets;
        }

        public static int GetDirectedLineMaxTargets(SkillDataSO skill)
        {
            if (skill == null) return 1;
            return skill.GetProjectileMaxTargets();
        }
    }
}
