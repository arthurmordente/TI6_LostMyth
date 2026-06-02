using Logic.Scripts.GameDomain.Services.Skills;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout
{
    public enum ExplorationLoadoutSkillFilter
    {
        All = 0,
        Passive = 1,
        Damage = 2,
        Movement = 3,
        Buff = 4
    }

    public static class ExplorationLoadoutSkillFilterUtil
    {
        public static bool Matches(SkillDataSO skill, ExplorationLoadoutSkillFilter filter)
        {
            if (skill == null) return false;
            switch (filter)
            {
                case ExplorationLoadoutSkillFilter.All:
                    return true;
                case ExplorationLoadoutSkillFilter.Passive:
                    return skill.SkillType == SkillType.Passive;
                case ExplorationLoadoutSkillFilter.Damage:
                    return skill.SkillType == SkillType.Damage;
                case ExplorationLoadoutSkillFilter.Movement:
                    return skill.SkillType == SkillType.Movement;
                case ExplorationLoadoutSkillFilter.Buff:
                    return skill.SkillType == SkillType.SelfBuff;
                default:
                    return true;
            }
        }

        /// <summary>Order in "Todas": Passivas → Dano → Movimento → Buff (depois nome).</summary>
        public static int AllViewSortGroup(SkillType type)
        {
            switch (type)
            {
                case SkillType.Passive: return 0;
                case SkillType.Damage: return 1;
                case SkillType.Movement: return 2;
                case SkillType.SelfBuff: return 3;
                default: return 99;
            }
        }

        public static string DisplayLabel(ExplorationLoadoutSkillFilter filter)
        {
            switch (filter)
            {
                case ExplorationLoadoutSkillFilter.All: return "Todas";
                case ExplorationLoadoutSkillFilter.Passive: return "Passivas";
                case ExplorationLoadoutSkillFilter.Damage: return "Dano";
                case ExplorationLoadoutSkillFilter.Movement: return "Movimento";
                case ExplorationLoadoutSkillFilter.Buff: return "Buff";
                default: return "Todas";
            }
        }

        public static string DisplayLabel(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Damage: return "Dano";
                case SkillType.SelfBuff: return "Buff";
                case SkillType.Movement: return "Movimento";
                case SkillType.Passive: return "Passiva";
                default: return "-";
            }
        }
    }
}
