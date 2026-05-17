using Logic.Scripts.Turns;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>
    /// Tile AP +/- is resolved at end of turn but must affect the <b>next</b> player phase
    /// (after <see cref="IActionPointsService.GainTurnPoints"/>).
    /// </summary>
    public static class LakiArenaTileActionPointsBridge
    {
        static int _pendingPlayerApDelta;

        public static void EnqueuePlayerDelta(int delta)
        {
            if (delta == 0) return;
            _pendingPlayerApDelta += delta;
            Debug.Log($"[LakiTileAP] Queued delta {delta} (pending total={_pendingPlayerApDelta}) for next PlayerAct");
        }

        public static bool ApplyPendingToPlayer(IActionPointsService actionPoints)
        {
            if (actionPoints == null || _pendingPlayerApDelta == 0) return false;

            int delta = _pendingPlayerApDelta;
            _pendingPlayerApDelta = 0;
            int before = actionPoints.Current;

            if (delta > 0)
                actionPoints.Add(delta);
            else
                actionPoints.Subtract(-delta);

            Debug.Log($"[LakiTileAP] Applied pending {delta} → AP {before} → {actionPoints.Current}/{actionPoints.Max}");
            return true;
        }

        public static void Reset() => _pendingPlayerApDelta = 0;
    }
}
