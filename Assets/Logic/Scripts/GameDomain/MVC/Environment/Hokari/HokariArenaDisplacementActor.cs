using System.Threading.Tasks;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
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
        readonly CombatAttackVisualCatalogSO _visualCatalog;
        readonly Vector3 _arenaCenter;
        readonly float _arenaRadiusXZ;

        public bool RemoveAfterRun => false;

        public HokariArenaDisplacementActor(
            ITurnStateReader turnState,
            INaraController nara,
            HokariArenaHazardPatternSO pattern,
            CombatAttackVisualCatalogSO visualCatalog,
            Vector3 arenaCenter,
            float arenaRadiusXZ)
        {
            _turnState = turnState;
            _nara = nara;
            _pattern = pattern;
            _visualCatalog = visualCatalog;
            _arenaCenter = arenaCenter;
            _arenaRadiusXZ = arenaRadiusXZ;
        }

        /// <summary>Environment phase: only spawn telegraph for a future turn (stays until that turn's strike).</summary>
        public async Task ExecuteAsync()
        {
            if (_pattern == null) return;
            int turn = _turnState != null ? _turnState.TurnNumber : 0;
            await PrepareTelegraphForTurnAsync(turn + 1);
        }

        public Task PrepareTelegraphForTurnAsync(int executionTurn)
        {
            PrepareTelegraphForTurn(executionTurn);
            return Task.CompletedTask;
        }

        public async Task ExecuteScheduledForTurnAsync(int executionTurn)
        {
            if (_pattern == null) return;
            await ExecuteDisplacementForCurrentTurn(executionTurn);
        }

        void PrepareTelegraphForTurn(int executionTurn)
        {
            if (executionTurn < 1 || !_pattern.HasAnyForTurn(executionTurn))
                return;

            HokariArenaHazardDefinitionSO picked = _pattern.PickRandomForTurn(executionTurn);
            if (picked == null) return;

            var telegraph = HokariArenaHazardTelegraphSpawner.Spawn(
                picked,
                _pattern,
                _visualCatalog,
                _arenaCenter,
                _arenaRadiusXZ,
                _nara,
                out Vector3 pullAnchor);

            HokariArenaHazardRuntimeSchedule.Commit(executionTurn, picked, pullAnchor);
            HokariArenaHazardRuntimeSchedule.SetActiveTelegraph(telegraph);
        }

        async Task ExecuteDisplacementForCurrentTurn(int executionTurn)
        {
            HokariArenaHazardDefinitionSO definition;
            Vector3 pullAnchor;

            if (HokariArenaHazardRuntimeSchedule.TryConsumeCommitted(executionTurn, out definition, out pullAnchor))
            {
                // Telegraph stays visible until the push finishes.
            }
            else
            {
                definition = _pattern.PickRandomForTurn(executionTurn);
                if (definition != null
                    && !HokariArenaHazardTelegraphSpawner.TryResolvePullAnchor(
                        definition, _arenaCenter, _arenaRadiusXZ, _nara, out pullAnchor))
                {
                    pullAnchor = _arenaCenter;
                }
            }

            if (definition == null)
            {
                HokariArenaHazardRuntimeSchedule.DestroyActiveTelegraph();
                return;
            }

            if (definition.DelayBeforePushSeconds > 0f)
                await Task.Delay(Mathf.RoundToInt(definition.DelayBeforePushSeconds * 1000f));

            PlanarPushRequest push = definition.ResolvePush(pullAnchor);
            TryPushNara(push, definition.Push.MultiplyByDebuffStacks);
            if (definition.ApplyToBook)
                TryPushBook(push);

            HokariArenaHazardRuntimeSchedule.DestroyActiveTelegraph();
        }

        void TryPushNara(in PlanarPushRequest push, bool multiplyStacks)
        {
            if (_nara?.NaraViewGO == null) return;
            var view = _nara.NaraViewGO.GetComponent<NaraView>();
            if (view == null) return;
            var rb = view.GetRigidbody();
            if (rb == null) return;

            IEffectable stacksTarget = multiplyStacks ? _nara as IEffectable : null;
            ArenaPlanarDisplacement.TryApply(rb, in push, stacksTarget);
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

    /// <summary>
    /// Committed hazard for a future environment turn (telegraph on T−1, push on T).
    /// </summary>
    public static class HokariArenaHazardRuntimeSchedule
    {
        static int _committedExecutionTurn = -1;
        static HokariArenaHazardDefinitionSO _committedDefinition;
        static Vector3 _committedPullAnchor;
        static GameObject _activeTelegraph;

        public static void ClearAll()
        {
            DestroyActiveTelegraph();
            _committedExecutionTurn = -1;
            _committedDefinition = null;
            _committedPullAnchor = Vector3.zero;
        }

        public static void Commit(int executionTurn, HokariArenaHazardDefinitionSO definition, Vector3 pullAnchorWorld)
        {
            _committedExecutionTurn = executionTurn;
            _committedDefinition = definition;
            _committedPullAnchor = pullAnchorWorld;
        }

        public static bool TryConsumeCommitted(
            int executionTurn,
            out HokariArenaHazardDefinitionSO definition,
            out Vector3 pullAnchorWorld)
        {
            definition = null;
            pullAnchorWorld = Vector3.zero;
            if (_committedDefinition == null || _committedExecutionTurn != executionTurn)
                return false;
            definition = _committedDefinition;
            pullAnchorWorld = _committedPullAnchor;
            _committedDefinition = null;
            _committedExecutionTurn = -1;
            _committedPullAnchor = Vector3.zero;
            return true;
        }

        public static void SetActiveTelegraph(GameObject instance)
        {
            DestroyActiveTelegraph();
            _activeTelegraph = instance;
        }

        public static void DestroyActiveTelegraph()
        {
            if (_activeTelegraph == null) return;
            Object.Destroy(_activeTelegraph);
            _activeTelegraph = null;
        }

        /// <summary>Read-only preview for debug gizmos (committed hazard not yet consumed).</summary>
        public static bool TryGetActiveCommit(out int executionTurn, out Vector3 pullAnchorWorld, out float telegraphDiscRadius)
        {
            executionTurn = _committedExecutionTurn;
            pullAnchorWorld = _committedPullAnchor;
            telegraphDiscRadius = _committedDefinition != null ? _committedDefinition.TelegraphDiscRadius : 0f;
            return _committedDefinition != null && _committedExecutionTurn >= 1;
        }
    }
}
