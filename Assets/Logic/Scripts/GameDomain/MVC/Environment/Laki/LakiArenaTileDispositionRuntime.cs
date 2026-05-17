using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>
    /// Links <see cref="LakiArenaPhaseTileDispositionSO"/> to <see cref="RouletteArenaService"/> and reacts to boss phase changes.
    /// </summary>
    public static class LakiArenaTileDispositionRuntime
    {
        static RouletteArenaService _arena;
        static LakiArenaPhaseTileDispositionSO _table;
        static int _appliedPhaseIndex = -1;

        public static bool IsRegistered => _arena != null && _table != null;

        public static void Register(
            RouletteArenaService arena,
            LakiArenaPhaseTileDispositionSO table)
        {
            _arena = arena;
            _table = table;
            _appliedPhaseIndex = -1;
            ApplyPhase(0);
        }

        public static void NotifyBossPhaseChanged(int phaseIndex) => ApplyPhase(phaseIndex);

        public static void Clear()
        {
            _arena = null;
            _table = null;
            _appliedPhaseIndex = -1;
        }

        static void ApplyPhase(int phaseIndex)
        {
            if (_arena == null || _table == null) return;
            if (phaseIndex == _appliedPhaseIndex) return;
            _appliedPhaseIndex = phaseIndex;
            LakiArenaTileDisposition disposition = _table.Resolve(phaseIndex);
            _arena.SetTileDisposition(disposition);
            Debug.Log(
                $"[LakiArena] Tile disposition phase {phaseIndex}: " +
                $"red={disposition.NegativeCount} grey={disposition.NeutralCount} green={disposition.PositiveCount}");
        }
    }
}
