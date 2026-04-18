using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Cast.Paschoal {
    /// <summary>
    /// Derives how Paschoal aim preview should resolve targets from <see cref="SkillDataSO"/> data,
    /// without per-asset inspector overrides (zero serialized changes on existing skills).
    /// </summary>
    public static class PaschoalSkillTargetingRules {
        public static PaschoalAimHighlightKind GetHighlightKind(SkillDataSO skill) {
            if (skill == null) return PaschoalAimHighlightKind.None;
            if (skill.AreaOfEffect > 0.0001f) return PaschoalAimHighlightKind.GroundAreaSphere;
            if (skill.AttackPrefab != null) return PaschoalAimHighlightKind.DirectedLine;
            return PaschoalAimHighlightKind.None;
        }

        /// <summary>
        /// When true, every non-player <see cref="IEffectable"/> along the aim segment may be highlighted.
        /// When false, only the first such target along the ray is highlighted (stops at first hittable).
        /// Inferred from <see cref="Projectile"/> on <see cref="SkillDataSO.AttackPrefab"/> when present;
        /// otherwise defaults to first-target-only for directed skills.
        /// </summary>
        public static bool GetDirectedLineUsesPierce(SkillDataSO skill) {
            if (skill?.AttackPrefab == null) return false;
            var projectile = skill.AttackPrefab.GetComponent<Projectile>();
            if (projectile == null) return false;
            return projectile.PaschoalAimUsesPiercingLineHighlight;
        }
    }
}
