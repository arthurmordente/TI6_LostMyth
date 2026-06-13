using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers
{
    public interface IFloatingCombatNumberService
    {
        void Show(Transform anchor, int amount, FloatingCombatNumberKind kind);
    }
}
