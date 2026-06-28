using Logic.Scripts.GameDomain.MVC.Boss;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    public static class DiceAttackRuntimeService
    {
        /// <summary>Opens the Laki shield vulnerability window — no direct HP loss from dice.</summary>
        public static void NotifyPlayerWonDiceOpensShieldWindow(
            in DiceAttackResult result,
            int fightTurnNumber,
            BossPhasesSO phases = null,
            int currentPhaseIndex = 0,
            int bossMaxHealth = 0)
        {
            if (!result.Completed || !result.PlayerWon) return;
            LakiBossShieldRuntime.RegisterDicePlayerWin(fightTurnNumber, phases, currentPhaseIndex, bossMaxHealth);
        }

        public interface IStatusProvider { string GetStatus(); }
        public interface IResolver
        {
            bool TryResolveAtBossTurn(out DiceAttackResult result);
            void DestroyDiceAttackRoot(bool deferUiDismiss = false);
        }
        public interface IPlayerTurnGate
        {
            System.Threading.Tasks.Task OnPlayerTurnStartAsync();
        }

        private static int _activeCount;
        private static bool _skipOnceOnBossTurn;
        private static bool _pauseBossOnce;
        private static bool _scoreboardDismissDeferred;
        private static readonly System.Collections.Generic.List<IResolver> _resolvers = new System.Collections.Generic.List<IResolver>(2);
        private static readonly System.Collections.Generic.List<IPlayerTurnGate> _playerTurnGates = new System.Collections.Generic.List<IPlayerTurnGate>(2);

        public static IStatusProvider StatusProvider { get; set; }
        public static string ActiveName { get; private set; }
        public static System.Action<string> OnNameChanged;
        /// <summary>Fired when the first DiceAttack session starts (count goes from 0 to 1).</summary>
        public static event System.Action OnDiceAttackBegan;
        /// <summary>Fired when the last active DiceAttack session ends.</summary>
        public static event System.Action OnDiceAttackEnded;
        /// <summary>Fired when the boss die is spawned and begins moving (DiceAttack flow).</summary>
        public static event System.Action<DiceActor> OnBossDieSpawned;
        /// <summary>Fired when the player die is spawned and begins moving (DiceAttack flow).</summary>
        public static event System.Action<DiceActor> OnPlayerDieSpawned;
        /// <summary>Fired when a die finishes its landing animation (DiceAttack flow).</summary>
        public static event System.Action<bool, int> OnDieLanded;
        /// <summary>Fired at the start of the player dice-turn gate (before roll prompt).</summary>
        public static event System.Action OnDicePlayerTurnOpening;
        /// <summary>Fired when the player roll phase begins (dice spawn).</summary>
        public static event System.Action OnPlayerRollPhaseStarted;
        public static bool IsActive => _activeCount > 0;

        /// <summary>Fired when a deferred scoreboard should hide (start of next Laki turn).</summary>
        public static event System.Action OnDeferredScoreboardDismiss;

        public static bool IsScoreboardDismissDeferred => _scoreboardDismissDeferred;

        public static void NotifyBossDieSpawned(DiceActor actor)
        {
            if (actor == null) return;
            try { OnBossDieSpawned?.Invoke(actor); } catch { }
        }

        public static void NotifyPlayerDieSpawned(DiceActor actor)
        {
            if (actor == null) return;
            try { OnPlayerDieSpawned?.Invoke(actor); } catch { }
        }

        public static void NotifyDieLanded(bool isBoss, int rollSlotIndex)
        {
            try { OnDieLanded?.Invoke(isBoss, rollSlotIndex); } catch { }
        }

        public static void NotifyPlayerRollPhaseStarted()
        {
            try { OnPlayerRollPhaseStarted?.Invoke(); } catch { }
        }

        public static void Begin()
        {
            bool wasInactive = _activeCount <= 0;
            _activeCount++;
            _scoreboardDismissDeferred = false;
            if (wasInactive) try { OnDiceAttackBegan?.Invoke(); } catch { }
        }

        public static void SetActiveName(string name)
        {
            ActiveName = name;
            try { OnNameChanged?.Invoke(ActiveName); } catch { }
        }

        public static void EndAndScheduleBossResolutionSkip(bool dismissScoreboard = true)
        {
            if (_activeCount > 0) _activeCount--;
            _skipOnceOnBossTurn = true;
            _pauseBossOnce = true;
            if (!dismissScoreboard)
                _scoreboardDismissDeferred = true;
            if (_activeCount <= 0)
            {
                StatusProvider = null;
                if (dismissScoreboard)
                    try { OnDiceAttackEnded?.Invoke(); } catch { }
            }
            SetActiveName(null);
        }

        public static bool TryDismissDeferredScoreboard()
        {
            if (!_scoreboardDismissDeferred) return false;
            _scoreboardDismissDeferred = false;
            try { OnDeferredScoreboardDismiss?.Invoke(); } catch { }
            try { OnDiceAttackEnded?.Invoke(); } catch { }
            return true;
        }

        public static bool ConsumeSkipOnBossTurn()
        {
            if (!_skipOnceOnBossTurn) return false;
            _skipOnceOnBossTurn = false;
            return true;
        }

        public static bool ConsumePauseBossThisTurn()
        {
            if (!_pauseBossOnce) return false;
            _pauseBossOnce = false;
            return true;
        }

        public static void RegisterResolver(IResolver resolver)
        {
            if (resolver == null) return;
            if (!_resolvers.Contains(resolver)) _resolvers.Add(resolver);
        }

        public static void UnregisterResolver(IResolver resolver)
        {
            if (resolver == null) return;
            _resolvers.Remove(resolver);
        }

        public static bool TryResolveAnyAtBossTurn(out DiceAttackResult result, out IResolver resolver)
        {
            for (int i = 0; i < _resolvers.Count; i++)
            {
                var r = _resolvers[i];
                if (r == null) continue;
                if (r.TryResolveAtBossTurn(out result))
                {
                    resolver = r;
                    return true;
                }
            }
            result = default;
            resolver = null;
            return false;
        }

        public static void RegisterPlayerTurnGate(IPlayerTurnGate gate)
        {
            if (gate == null) return;
            if (!_playerTurnGates.Contains(gate)) _playerTurnGates.Add(gate);
        }

        public static void UnregisterPlayerTurnGate(IPlayerTurnGate gate)
        {
            if (gate == null) return;
            _playerTurnGates.Remove(gate);
        }

        public static async System.Threading.Tasks.Task RunPlayerTurnGatesAsync()
        {
            if (_playerTurnGates.Count == 0) return;
            if (IsActive)
            {
                try { OnDicePlayerTurnOpening?.Invoke(); } catch { }
            }
            var snapshot = new System.Collections.Generic.List<IPlayerTurnGate>(_playerTurnGates);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var gate = snapshot[i];
                if (gate == null) continue;
                try { await gate.OnPlayerTurnStartAsync(); } catch { }
            }
        }

        public static void Reset()
        {
            bool was = _activeCount > 0;
            _activeCount = 0;
            _skipOnceOnBossTurn = false;
            _pauseBossOnce = false;
            _scoreboardDismissDeferred = false;
            _resolvers.Clear();
            _playerTurnGates.Clear();
            StatusProvider = null;
            SetActiveName(null);
            if (was) try { OnDiceAttackEnded?.Invoke(); } catch { }
        }
    }
}
