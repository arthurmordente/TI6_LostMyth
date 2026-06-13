using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers;
using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback
{
    public static class ManaGainFloatingFeedback
    {
        public static void TryShow(Transform anchor, int amountGained)
        {
            TryShowWithKind(anchor, amountGained, FloatingCombatNumberKind.ManaGain);
        }

        public static void TryShowOnPlayer(int amountGained)
        {
            TryShowOnPlayerWithKind(amountGained, FloatingCombatNumberKind.ManaGain);
        }

        public static void TryShowOnPlayerWithKind(int amount, FloatingCombatNumberKind kind)
        {
            if (amount <= 0) return;
            TryShowWithKind(ResolvePlayerTransform(), amount, kind);
        }

        public static void TryShowWithKind(Transform anchor, int amount, FloatingCombatNumberKind kind)
        {
            if (anchor == null || amount <= 0) return;
            FloatingCombatNumberBridge.Show(anchor, amount, kind);
        }

        public static Transform ResolvePlayerTransform()
        {
            var nara = Object.FindFirstObjectByType<NaraView>();
            if (nara != null) return nara.transform;

            var book = Object.FindFirstObjectByType<Logic.Scripts.GameDomain.MVC.Book.BookView>();
            return book != null ? book.transform : null;
        }
    }
}