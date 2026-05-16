using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment
{
    /// <summary>
    /// Ring-out loss when player (and optionally book) leaves Hokari arena safe zone.
    /// </summary>
    public sealed class CombatArenaEliminationWatcher : IFixedUpdatable
    {
        readonly IUpdateSubscriptionService _updates;
        readonly ICommandFactory _commandFactory;

        int _naraOutsideFrames;
        int _bookOutsideFrames;
        bool _lossTriggered;

        public CombatArenaEliminationWatcher(
            IUpdateSubscriptionService updates,
            ICommandFactory commandFactory)
        {
            _updates = updates;
            _commandFactory = commandFactory;
        }

        public void Register()
        {
            _lossTriggered = false;
            _naraOutsideFrames = 0;
            _bookOutsideFrames = 0;
            _updates.RegisterFixedUpdatable(this);
        }

        public void Unregister()
        {
            _updates.UnregisterFixedUpdatable(this);
        }

        public void ManagedFixedUpdate()
        {
            if (_lossTriggered || !CombatArenaBoundaryRuntime.EnableRingOutLoss) return;
            if (!CombatArenaBoundaryRuntime.TryGetHokariGeometry(out _)) return;

            if (TryTrackUnit(FindNaraPosition(), ref _naraOutsideFrames))
            {
                TriggerLoss();
                return;
            }

            if (TryTrackUnit(FindBookPosition(), ref _bookOutsideFrames))
                TriggerLoss();
        }

        bool TryTrackUnit(Vector3? pos, ref int consecutiveOutside)
        {
            if (!pos.HasValue) return false;
            Vector3 p = pos.Value;

            if (CombatArenaBoundaryRuntime.IsInsideVoluntaryZone(p))
            {
                consecutiveOutside = 0;
                return false;
            }

            consecutiveOutside++;
            return CombatArenaBoundaryRuntime.ShouldTriggerRingOut(p, consecutiveOutside);
        }

        void TriggerLoss()
        {
            _lossTriggered = true;
            try
            {
                _commandFactory.CreateCommandVoid<ArenaRingOutCommand>().Execute();
            }
            catch
            {
                _commandFactory.CreateCommandVoid<GameOverCommand>()
                    .SetData(new GameOverCommandData(false))
                    .Execute();
            }
        }

        static Vector3? FindNaraPosition()
        {
            try
            {
                var nara = Object.FindFirstObjectByType<NaraView>();
                if (nara == null) return null;
                var rb = nara.GetRigidbody();
                return rb != null ? rb.position : nara.transform.position;
            }
            catch { return null; }
        }

        static Vector3? FindBookPosition()
        {
            try
            {
                var book = Object.FindFirstObjectByType<BookView>();
                if (book == null) return null;
                var rb = book.GetRigidbody();
                return rb != null ? rb.position : book.transform.position;
            }
            catch { return null; }
        }
    }

}
