using System;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki
{
    /// <summary>Cross-cutting arena presentation hooks (SFX + animation) for Laki minigame/dice outcomes.</summary>
    public static class LakiArenaPresentationEvents
    {
        public static event Action<bool> OnBetResolved;

        public static void NotifyBetResolved(bool playerWon)
        {
            try { OnBetResolved?.Invoke(playerWon); } catch { }
        }
    }
}
