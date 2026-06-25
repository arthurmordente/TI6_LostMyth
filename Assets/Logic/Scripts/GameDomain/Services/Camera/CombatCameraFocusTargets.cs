using Logic.Scripts.GameDomain.MVC.Boss;
using Logic.Scripts.GameDomain.MVC.Boss.Laki;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Camera
{
    public static class CombatCameraFocusTargets
    {
        public static Transform ResolveBoss(BossController bossController)
        {
            var bridge = Object.FindFirstObjectByType<LakiBossAnimationBridge>();
            if (bridge != null) return bridge.transform;

            if (bossController != null)
            {
                try
                {
                    Transform reference = bossController.GetReferenceTransform();
                    if (reference != null) return reference;
                }
                catch { }
            }

            var bossView = Object.FindFirstObjectByType<BossView>();
            return bossView != null ? bossView.transform : null;
        }

        public static Transform ResolvePlayer(INaraController player)
        {
            if (player != null)
            {
                try
                {
                    var go = player.NaraViewGO;
                    if (go != null) return go.transform;
                }
                catch { }
            }

            var naraView = Object.FindFirstObjectByType<NaraView>();
            return naraView != null ? naraView.transform : null;
        }

        public static Transform ResolveActiveUnit(IActiveUnitService activeUnitService, INaraController playerFallback)
        {
            if (activeUnitService?.ActiveUnit != null)
            {
                var go = activeUnitService.ActiveUnit.UnitViewGO;
                if (go != null) return go.transform;
            }

            return ResolvePlayer(playerFallback);
        }
    }
}
