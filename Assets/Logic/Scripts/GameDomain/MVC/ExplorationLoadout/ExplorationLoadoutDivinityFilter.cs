using Logic.Scripts.GameDomain.Services.Skills;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout {
    public enum ExplorationLoadoutDivinityFilter {
        All = 0,
        Hocari = 1,
        Laki = 2,
        Ouroboros = 3,
        Mafdet = 4,
        Iara = 5
    }

    public static class ExplorationLoadoutDivinityFilterUtil {
        public static bool Matches(SkillDataSO skill, ExplorationLoadoutDivinityFilter filter) {
            if (skill == null) return false;
            if (filter == ExplorationLoadoutDivinityFilter.All) return true;
            return skill.Divinity == ToDivinity(filter);
        }

        public static SkillDivinity ToDivinity(ExplorationLoadoutDivinityFilter filter) {
            return filter switch {
                ExplorationLoadoutDivinityFilter.Hocari => SkillDivinity.Hocari,
                ExplorationLoadoutDivinityFilter.Laki => SkillDivinity.Laki,
                ExplorationLoadoutDivinityFilter.Ouroboros => SkillDivinity.Ouroboros,
                ExplorationLoadoutDivinityFilter.Mafdet => SkillDivinity.Mafdet,
                ExplorationLoadoutDivinityFilter.Iara => SkillDivinity.Iara,
                _ => SkillDivinity.Hocari
            };
        }

        public static string DisplayLabel(ExplorationLoadoutDivinityFilter filter) {
            if (filter == ExplorationLoadoutDivinityFilter.All) return "Todas";
            return SkillDivinityUtil.DisplayLabel(ToDivinity(filter));
        }
    }
}
