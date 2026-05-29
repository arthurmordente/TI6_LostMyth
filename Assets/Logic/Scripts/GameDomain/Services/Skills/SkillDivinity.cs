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
    }
}
