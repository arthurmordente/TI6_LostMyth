using System;
using System.Threading.Tasks;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari
{
    /// <summary>
    /// Turn-flow hook registered by <see cref="HokariArenaBossBootstrap"/>.
    /// </summary>
    public static class HokariArenaHazardTurnBridge
    {
        static HokariArenaDisplacementActor _actor;

        public static bool IsRegistered => _actor != null;

        public static void Register(HokariArenaDisplacementActor actor) => _actor = actor;

        public static void Unregister()
        {
            _actor = null;
            HokariArenaHazardRuntimeSchedule.ClearAll();
        }

        public static Task ExecuteScheduledForTurnAsync(int turnNumber) =>
            _actor != null ? _actor.ExecuteScheduledForTurnAsync(turnNumber) : Task.CompletedTask;

        public static Task PrepareTelegraphForTurnAsync(int executionTurn) =>
            _actor != null ? _actor.PrepareTelegraphForTurnAsync(executionTurn) : Task.CompletedTask;
    }
}
