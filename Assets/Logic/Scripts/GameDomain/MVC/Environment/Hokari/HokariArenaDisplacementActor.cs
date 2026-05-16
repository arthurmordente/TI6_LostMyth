using System.Threading.Tasks;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Turns;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari
{
    public sealed class HokariArenaDisplacementActor : IEnvironmentTurnActor
    {
        readonly ITurnStateReader _turnState;
        readonly INaraController _nara;
        readonly HokariArenaHazardPatternSO _pattern;
        readonly Vector3 _arenaCenter;

        public bool RemoveAfterRun => false;

        public HokariArenaDisplacementActor(
            ITurnStateReader turnState,
            INaraController nara,
            HokariArenaHazardPatternSO pattern,
            Vector3 arenaCenter)
        {
            _turnState = turnState;
            _nara = nara;
            _pattern = pattern;
            _arenaCenter = arenaCenter;
        }

        public async Task ExecuteAsync()
        {
            if (_pattern == null) return;
            int turn = _turnState != null ? _turnState.TurnNumber : 0;
            if (!_pattern.TryGetEntryForTurn(turn, out HokariArenaHazardTurnEntry entry))
                return;

            if (entry.DelayBeforePushSeconds > 0f)
                await Task.Delay(Mathf.RoundToInt(entry.DelayBeforePushSeconds * 1000f));

            PlanarPushRequest push = entry.Push;
            if (push.ReferenceWorldPoint == Vector3.zero && push.DirectionMode is ArenaPlanarDirectionMode.RadialOutFromPoint or ArenaPlanarDirectionMode.RadialInToPoint)
                push.ReferenceWorldPoint = _arenaCenter;

            TryPushNara(push);
            if (entry.ApplyToBook)
                TryPushBook(push);

            await Task.CompletedTask;
        }

        void TryPushNara(in PlanarPushRequest push)
        {
            if (_nara?.NaraViewGO == null) return;
            var view = _nara.NaraViewGO.GetComponent<NaraView>();
            if (view == null) return;
            var rb = view.GetRigidbody();
            if (rb == null) return;
            ArenaPlanarDisplacement.TryApply(rb, in push, _nara as IEffectable);
        }

        void TryPushBook(in PlanarPushRequest push)
        {
            try
            {
                var bookView = Object.FindFirstObjectByType<BookView>();
                if (bookView == null) return;
                var rb = bookView.GetRigidbody();
                if (rb == null) return;
                ArenaPlanarDisplacement.TryApply(rb, in push);
            }
            catch { }
        }
    }
}
