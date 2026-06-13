using Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers;
using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback
{
    /// <summary>Mana removed via <see cref="Logic.Scripts.Turns.IActionPointsService.Subtract"/> — not skill spend.</summary>
    public static class ManaLostFloatingFeedback
    {
        public static void TryShow(Transform anchor, int amountLost)
        {
            if (amountLost <= 0) return;
            ManaGainFloatingFeedback.TryShowWithKind(anchor, amountLost, FloatingCombatNumberKind.ManaLost);
        }

        public static void TryShowOnPlayer(int amountLost)
        {
            if (amountLost <= 0) return;
            TryShow(ManaGainFloatingFeedback.ResolvePlayerTransform(), amountLost);
        }
    }
}
