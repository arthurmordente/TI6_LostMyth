using Logic.Scripts.GameDomain.Services.Skills;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout
{
    /// <summary>Active catalog drag session — used by drop targets when pointerDrag hierarchy is ambiguous.</summary>
    public static class LoadoutDragContext
    {
        public static SkillDataSO DraggingSkill { get; private set; }
        public static bool IsDragging => DraggingSkill != null;

        public static void Begin(SkillDataSO skill) => DraggingSkill = skill;

        public static void Clear() => DraggingSkill = null;
    }
}
