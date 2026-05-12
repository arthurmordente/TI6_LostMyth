using Assets.Logic.Scripts.GameDomain.Effects;
using Logic.Scripts.GameDomain.MVC.Boss;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>
    /// Laki arena only: shield VFX + immunity while active. After the player wins DiceAttack on fight turn T,
    /// the shield turns off for that same fight turn T and T+1 (vulnerable window), then turns on again from T+2 onward.
    /// Wired from <see cref="LakiArenaBossBootstrap"/> and dice resolution (no HP loss from dice).
    /// </summary>
    public static class LakiBossShieldRuntime
    {
        private static GameObject _shieldRoot;
        private static bool _registered;
        private static bool _hasVulnWindow;
        private static int _vulnMinTurnInclusive;
        private static int _vulnMaxTurnInclusive;
        private static int _lastSyncedTurn = -1;

        public static void RegisterShieldRoot(GameObject shieldRoot)
        {
            _shieldRoot = shieldRoot;
            _registered = shieldRoot != null;
            if (_shieldRoot != null)
                _shieldRoot.SetActive(true);
        }

        public static void Reset()
        {
            _shieldRoot = null;
            _registered = false;
            _hasVulnWindow = false;
            _vulnMinTurnInclusive = 0;
            _vulnMaxTurnInclusive = 0;
            _lastSyncedTurn = -1;
        }

        public static void SyncFightTurn(int fightTurnNumber)
        {
            _lastSyncedTurn = fightTurnNumber;
            ApplyShieldVisualForCurrentState();
        }

        public static void RegisterDicePlayerWin(int resolvedDuringFightTurn)
        {
            _hasVulnWindow = true;
            // Inclusive window: same fight turn as resolution + next turn (two boss/player cycles vulnerable).
            _vulnMinTurnInclusive = resolvedDuringFightTurn;
            _vulnMaxTurnInclusive = resolvedDuringFightTurn + 1;
            // Bootstrap may not have run SyncFightTurn yet this frame; use resolving turn for immediate VFX.
            _lastSyncedTurn = resolvedDuringFightTurn;
            ApplyShieldVisualForCurrentState();
        }

        private static bool IsVulnerableForTurn(int turn)
        {
            if (!_hasVulnWindow) return false;
            return turn >= _vulnMinTurnInclusive && turn <= _vulnMaxTurnInclusive;
        }

        private static bool ShouldShowShieldVisual()
        {
            if (!_registered || _shieldRoot == null) return false;
            if (_lastSyncedTurn < 0) return true;
            return !IsVulnerableForTurn(_lastSyncedTurn);
        }

        private static void ApplyShieldVisualForCurrentState()
        {
            if (_shieldRoot == null) return;
            _shieldRoot.SetActive(ShouldShowShieldVisual());
        }

        public static bool IsLakiShieldBlockingCombatInteraction()
        {
            if (!_registered) return false;
            if (_lastSyncedTurn < 0) return true;
            return !IsVulnerableForTurn(_lastSyncedTurn);
        }

        public static bool ShouldSuppressNewSkillSystemHighlightFor(IEffectable effectable)
        {
            if (!IsLakiShieldBlockingCombatInteraction()) return false;
            return ResolvesToBoss(effectable);
        }

        private static bool ResolvesToBoss(IEffectable e)
        {
            if (e == null) return false;
            if (e is BossView) return true;
            if (e is BossController) return true;
            if (e is EffectableRelay relay)
                return ResolvesToBoss(relay.ForwardTarget);
            return false;
        }
    }
}
