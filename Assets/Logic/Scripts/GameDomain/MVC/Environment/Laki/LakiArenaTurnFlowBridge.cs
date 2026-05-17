using System;
using System.Threading.Tasks;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>
    /// Turn-flow hook registered by <see cref="LakiArenaBossBootstrap"/>.
    /// End of turn: apply tile effect → delay → boss resolve → delay → reroll.
    /// Start of turn: boss prepare only (see <see cref="Turns.TurnFlowController"/>).
    /// </summary>
    public static class LakiArenaTurnFlowBridge
    {
        static ILakiRouletteArenaTurnPhases _arenaPhases;
        static float _postApplyBeforeBossResolveSeconds = 2f;
        static float _postBossBeforeRerollSeconds = 2f;

        public static bool IsRegistered => _arenaPhases != null;

        public static float PostApplyBeforeBossResolveSeconds => _postApplyBeforeBossResolveSeconds;

        public static float PostBossBeforeRerollSeconds => _postBossBeforeRerollSeconds;

        public static void Register(
            ILakiRouletteArenaTurnPhases arenaPhases,
            float postApplyBeforeBossResolveSeconds,
            float postBossBeforeRerollSeconds)
        {
            _arenaPhases = arenaPhases;
            _postApplyBeforeBossResolveSeconds = Math.Max(0f, postApplyBeforeBossResolveSeconds);
            _postBossBeforeRerollSeconds = Math.Max(0f, postBossBeforeRerollSeconds);
        }

        public static void Unregister()
        {
            _arenaPhases = null;
        }

        public static Task ExecuteApplyPhaseAsync() =>
            _arenaPhases != null ? _arenaPhases.ExecuteApplyPhaseAsync() : Task.CompletedTask;

        public static Task ExecuteRerollPhaseAsync() =>
            _arenaPhases != null ? _arenaPhases.ExecuteRerollPhaseAsync() : Task.CompletedTask;

        public static Task DelayPostApplyAsync() =>
            DelaySecondsAsync(_postApplyBeforeBossResolveSeconds);

        public static Task DelayPostBossAsync() =>
            DelaySecondsAsync(_postBossBeforeRerollSeconds);

        static async Task DelaySecondsAsync(float seconds)
        {
            if (seconds <= 0f) return;
            int ms = (int)(seconds * 1000f);
            if (ms > 0) await Task.Delay(ms);
        }
    }

    public interface ILakiRouletteArenaTurnPhases
    {
        Task ExecuteApplyPhaseAsync();
        Task ExecuteRerollPhaseAsync();
    }
}
