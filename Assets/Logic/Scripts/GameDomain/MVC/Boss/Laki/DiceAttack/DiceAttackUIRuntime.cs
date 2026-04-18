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
        /// <summary>Fired when a player die begins a roll/move (spawn or reroll). Dice prompt idle hint timers should restart.</summary>
        public static event Action OnPlayerRollIdleHintReset;

        public static void NotifyPlayerRollPromptShow()
        {
            try { OnPlayerRollPromptShow?.Invoke(); } catch { }
        }

        public static void NotifyPlayerRollPromptHide()
        {
            try { OnPlayerRollPromptHide?.Invoke(); } catch { }
        }

        public static void NotifyPlayerRollIdleHintReset()
        {
            try { OnPlayerRollIdleHintReset?.Invoke(); } catch { }
        }
    }
}
