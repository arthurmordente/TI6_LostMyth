using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki
{
    /// <summary>
    /// Laki animation phases:
    /// Prepare — sorteia idle 2/3, Prep→Loop até ao resolve.
    /// Resolve — Finish/Ability; fica em Idle_1.
    /// Próximo prepare — novo sorteio (triggers resetados).
    /// </summary>
    public class LakiBossAnimationBridge : MonoBehaviour
    {
        [SerializeField] private LakiBossAnimatorView _view;
        [SerializeField] private int[] _turnPerformancePool = { 2, 3 };
        [SerializeField] private bool _includeIdle1InPool;

        private bool _performanceLoopActive;
        private Task _resolveAnimationTask = Task.CompletedTask;

        private void Awake()
        {
            if (_view == null)
                _view = GetComponent<LakiBossAnimatorView>();
            if (_view == null)
                _view = GetComponentInChildren<LakiBossAnimatorView>(true);
        }

        public async Task OnBossPrepareTurnStartedAsync()
        {
            await ExitPerformanceToBaseIdleAsync();
            await RollAndStartTurnIdleAsync();
        }

        public void BeginResolveAttackAnimation()
        {
            if (_view == null) return;
            _resolveAnimationTask = RunResolveAttackAnimationAsync();
        }

        public Task WaitForResolveAttackAnimationAsync() => _resolveAnimationTask ?? Task.CompletedTask;

        private async Task RunResolveAttackAnimationAsync()
        {
            if (_view == null) return;

            if (_performanceLoopActive)
            {
                _view.PlayPerformanceFinish();
                _performanceLoopActive = false;
                await _view.WaitUntilStateTagAsync(LakiAnimatorParams.TagIdle, 1.5f);
            }

            _view.PlayAbility();
            await _view.WaitUntilLeftStateTagAsync(LakiAnimatorParams.TagAbility, 4f);
            _performanceLoopActive = false;
        }

        private async Task ExitPerformanceToBaseIdleAsync()
        {
            if (_view == null) return;

            if (_performanceLoopActive)
            {
                _view.PlayPerformanceFinish();
                _performanceLoopActive = false;
                await _view.WaitUntilStateTagAsync(LakiAnimatorParams.TagIdle, 2.5f);
                return;
            }

            if (!_view.IsInPerformanceLoop())
                await _view.WaitUntilStateTagAsync(LakiAnimatorParams.TagIdle, 0.35f);
        }

        private async Task RollAndStartTurnIdleAsync()
        {
            if (_view == null) return;

            int pick = PickPerformanceForTurn();
            if (pick < 2)
            {
                _performanceLoopActive = false;
                _view.SetPerformanceLoop(false);
                return;
            }

            _view.BeginPerformanceTurn(pick);
            await _view.WaitUntilStateTagAsync(LakiAnimatorParams.TagPerformancePrep, 2f);
            _view.SetPerformanceLoop(true);
            _performanceLoopActive = true;

            await _view.WaitUntilStateTagAsync(LakiAnimatorParams.TagPerformanceLoop, 2f);
        }

        private int PickPerformanceForTurn()
        {
            if (_turnPerformancePool == null || _turnPerformancePool.Length == 0)
                return _includeIdle1InPool ? Random.Range(1, 4) : Random.Range(2, 4);

            for (int attempt = 0; attempt < 8; attempt++)
            {
                int pick = _turnPerformancePool[Random.Range(0, _turnPerformancePool.Length)];
                if (pick < 1) pick = 1;
                if (pick > 3) pick = 3;
                if (pick == 1 && !_includeIdle1InPool) continue;
                return pick;
            }

            return 2;
        }
    }
}
