using System;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    /// <summary>
    /// Hooks for optional UI when the player must confirm the dice roll (no prefab required).
    /// </summary>
    public static class DiceAttackUIRuntime
    {
        public static event Action OnPlayerRollPromptShow;
        public static event Action OnPlayerRollPromptHide;

        public static void NotifyPlayerRollPromptShow()
        {
            try { OnPlayerRollPromptShow?.Invoke(); } catch { }
        }

        public static void NotifyPlayerRollPromptHide()
        {
            try { OnPlayerRollPromptHide?.Invoke(); } catch { }
        }
    }
}
