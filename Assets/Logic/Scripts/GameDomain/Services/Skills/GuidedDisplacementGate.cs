namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Suppresses per-frame voluntary arena clamp on movement controllers while
    /// <see cref="ArenaSkillPathDisplacer"/> owns displacement (avoids sliding along the ring).
    /// </summary>
    public static class GuidedDisplacementGate
    {
        static int _depth;

        public static bool IsActive => _depth > 0;

        public static void Enter() => _depth++;

        public static void Exit()
        {
            if (_depth > 0) _depth--;
        }
    }
}
