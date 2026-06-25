using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills {
    public enum SkillDivinity {
        Hocari = 0,
        Laki = 1,
        Ouroboros = 2,
        Mafdet = 3,
        Iara = 4
    }

    public static class SkillDivinityUtil {
        public static readonly SkillDivinity[] AllValues = {
            SkillDivinity.Hocari,
            SkillDivinity.Laki,
            SkillDivinity.Ouroboros,
            SkillDivinity.Mafdet,
            SkillDivinity.Iara
        };

        public static string DisplayLabel(SkillDivinity divinity) {
            return divinity switch {
                SkillDivinity.Hocari => "Hocari",
                SkillDivinity.Laki => "Laki",
                SkillDivinity.Ouroboros => "Ouroboros",
                SkillDivinity.Mafdet => "Mafdet",
                SkillDivinity.Iara => "Iara",
                _ => divinity.ToString()
            };
        }

        /// <summary>Catalog sort order: Hocari → Laki → Ouroboros → Mafdet → Iara.</summary>
        public static int CatalogSortOrder(SkillDivinity divinity) => (int)divinity;

        /// <summary>Distinct debug color per divinity for skill hitbox gizmos.</summary>
        public static Color GetDebugHitboxColor(SkillDivinity divinity) {
            return divinity switch {
                SkillDivinity.Hocari => new Color(1f, 0.42f, 0.21f, 0.95f),
                SkillDivinity.Laki => new Color(1f, 0.84f, 0.1f, 0.95f),
                SkillDivinity.Ouroboros => new Color(0.61f, 0.35f, 0.71f, 0.95f),
                SkillDivinity.Mafdet => new Color(0f, 0.71f, 0.85f, 0.95f),
                SkillDivinity.Iara => new Color(0.9f, 0.22f, 0.27f, 0.95f),
                _ => new Color(1f, 1f, 1f, 0.9f)
            };
        }
    }
}
