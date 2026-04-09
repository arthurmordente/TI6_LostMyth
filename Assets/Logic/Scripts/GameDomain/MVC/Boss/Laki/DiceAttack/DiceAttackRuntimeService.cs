namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    public static class DiceAttackRuntimeService
    {
        public interface IStatusProvider { string GetStatus(); }
        public interface IResolver
        {
            bool TryResolveAtBossTurn(out DiceAttackResult result);
            void DestroyDiceAttackRoot();
        }
        public interface IPlayerTurnGate
        {
            System.Threading.Tasks.Task OnPlayerTurnStartAsync();
        }

        private static int _activeCount;
        private static bool _skipOnceOnBossTurn;
        private static bool _pauseBossOnce;
        private static readonly System.Collections.Generic.List<IResolver> _resolvers = new System.Collections.Generic.List<IResolver>(2);
        private static readonly System.Collections.Generic.List<IPlayerTurnGate> _playerTurnGates = new System.Collections.Generic.List<IPlayerTurnGate>(2);

        public static IStatusProvider StatusProvider { get; set; }
        public static string ActiveName { get; private set; }
        public static System.Action<string> OnNameChanged;
        /// <summary>Fired when the first DiceAttack session starts (count goes from 0 to 1).</summary>
        public static event System.Action OnDiceAttackBegan;
        /// <summary>Fired when the last active DiceAttack session ends.</summary>
        public static event System.Action OnDiceAttackEnded;
        public static bool IsActive => _activeCount > 0;

        public static void Begin()
        {
            bool wasInactive = _activeCount <= 0;
            _activeCount++;
            if (wasInactive) try { OnDiceAttackBegan?.Invoke(); } catch { }
        }

        public static void SetActiveName(string name)
        {
            ActiveName = name;
            try { OnNameChanged?.Invoke(ActiveName); } catch { }
        }

        public static void EndAndScheduleBossResolutionSkip()
        {
            if (_activeCount > 0) _activeCount--;
            _skipOnceOnBossTurn = true;
            _pauseBossOnce = true;
            if (_activeCount <= 0)
            {
                StatusProvider = null;
                try { OnDiceAttackEnded?.Invoke(); } catch { }
            }
            SetActiveName(null);
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
            _resolvers.Clear();
            _playerTurnGates.Clear();
            StatusProvider = null;
            SetActiveName(null);
            if (was) try { OnDiceAttackEnded?.Invoke(); } catch { }
        }
    }
}
