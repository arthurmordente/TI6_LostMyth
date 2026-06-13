using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers
{
    public static class FloatingCombatNumberBridge
    {
        static IFloatingCombatNumberService _service;

        public static void Bind(IFloatingCombatNumberService service) => _service = service;

        public static void Show(Transform anchor, int amount, FloatingCombatNumberKind kind)
        {
            if (anchor == null || amount <= 0 || _service == null) return;
            _service.Show(anchor, amount, kind);
        }
    }
}
