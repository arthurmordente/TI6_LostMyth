using Assets.Logic.Scripts.GameDomain.Effects;
using Logic.Scripts.GameDomain.MVC.Boss;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>
    /// Laki arena: shield VFX and damage gate stay in sync via <see cref="EngageShield"/> / <see cref="DisengageShield"/>.
    /// After the player wins dice, a vulnerability window opens for N fight turns. Only the dice-window HP loss cap
    /// limits damage; phase thresholds still drive behavior but do not clip damage or re-engage the shield early.
    /// </summary>
    public static class LakiBossShieldRuntime
    {
        public struct DamageFilterResult
        {
            public int AppliedDamage;
            public bool ShouldEngageShield;
            public int PhaseIndexAfterHit;
            public bool PhaseIndexChanged;
        }

        private static GameObject _shieldRoot;
        private static bool _registered;
        private static bool _shieldActive;
        private static bool _hasVulnWindow;
        private static int _vulnMinTurnInclusive;
        private static int _vulnMaxTurnInclusive;
        private static int _vulnerabilityFightTurnCount = 2;
        private static int _lastSyncedTurn = -1;
        private static int _damageTakenThisVulnerabilityWindow;
        private static int _maxDamageThisVulnerabilityWindow;
        private static float _maxHpFractionLossPerDiceWindow = 1f / 3f;

        public static void RegisterShieldRoot(GameObject shieldRoot)
        {
            _shieldRoot = shieldRoot;
            _registered = shieldRoot != null;
            if (_registered)
                EngageShield();
        }

        public static void ConfigureDamageCap(BossPhasesSO phases, int bossMaxHealth, int currentPhaseIndex = 0)
        {
            if (phases != null)
            {
                _vulnerabilityFightTurnCount = phases.VulnerabilityFightTurnCount;
                _maxHpFractionLossPerDiceWindow = phases.GetMaxHpFractionLossPerDiceWindowForPhase(currentPhaseIndex);
            }
            RecalculateMaxDamageThisVulnerabilityWindow(bossMaxHealth);
        }

        public static void Reset()
        {
            _shieldRoot = null;
            _registered = false;
            _shieldActive = false;
            _hasVulnWindow = false;
            _vulnMinTurnInclusive = 0;
            _vulnMaxTurnInclusive = 0;
            _vulnerabilityFightTurnCount = 2;
            _lastSyncedTurn = -1;
            _damageTakenThisVulnerabilityWindow = 0;
            _maxDamageThisVulnerabilityWindow = 0;
            _maxHpFractionLossPerDiceWindow = 1f / 3f;
        }

        public static void SyncFightTurn(int fightTurnNumber)
        {
            _lastSyncedTurn = fightTurnNumber;
            if (IsVulnerableForTurn(fightTurnNumber))
            {
                DisengageShield();
                return;
            }

            _damageTakenThisVulnerabilityWindow = 0;
            EngageShield();
        }

        public static void RegisterDicePlayerWin(int resolvedDuringFightTurn, BossPhasesSO phases = null, int currentPhaseIndex = 0, int bossMaxHealth = 0)
        {
            if (phases != null)
            {
                _vulnerabilityFightTurnCount = phases.VulnerabilityFightTurnCount;
                _maxHpFractionLossPerDiceWindow = phases.GetMaxHpFractionLossPerDiceWindowForPhase(currentPhaseIndex);
            }

            if (bossMaxHealth > 0)
                RecalculateMaxDamageThisVulnerabilityWindow(bossMaxHealth);

            _hasVulnWindow = true;
            _vulnMinTurnInclusive = resolvedDuringFightTurn;
            _vulnMaxTurnInclusive = resolvedDuringFightTurn + _vulnerabilityFightTurnCount - 1;
            _lastSyncedTurn = resolvedDuringFightTurn;
            _damageTakenThisVulnerabilityWindow = 0;
            DisengageShield();
        }

        public static void NotifyBossPhaseChanged(BossPhasesSO phases, int newPhaseIndex, int bossMaxHealth)
        {
            if (!_registered) return;

            if (phases != null)
                _maxHpFractionLossPerDiceWindow = phases.GetMaxHpFractionLossPerDiceWindowForPhase(newPhaseIndex);
            RecalculateMaxDamageThisVulnerabilityWindow(bossMaxHealth);

            if (_hasVulnWindow && IsVulnerableForTurn(_lastSyncedTurn))
            {
                Debug.Log(
                    $"[LakiShield] Phase {newPhaseIndex} during dice window — shield stays OFF. " +
                    $"Window damage={_damageTakenThisVulnerabilityWindow}/{_maxDamageThisVulnerabilityWindow} " +
                    $"(cap={_maxHpFractionLossPerDiceWindow:P0} max HP).");
                return;
            }

            _hasVulnWindow = false;
            EngageShield();
            Debug.Log(
                $"[LakiShield] Phase {newPhaseIndex} — shield ON. Window damage={_damageTakenThisVulnerabilityWindow}/{_maxDamageThisVulnerabilityWindow} " +
                $"(cap={_maxHpFractionLossPerDiceWindow:P0} max HP).");
        }

        public static DamageFilterResult FilterBossDamage(
            int requestedDamage,
            int bossMaxHealth,
            int currentHealth,
            BossPhasesSO phases,
            int currentPhaseIndex)
        {
            var result = new DamageFilterResult
            {
                AppliedDamage = requestedDamage,
                PhaseIndexAfterHit = currentPhaseIndex,
            };

            if (!_registered || requestedDamage <= 0)
                return result;

            if (bossMaxHealth > 0)
                RecalculateMaxDamageThisVulnerabilityWindow(bossMaxHealth);

            if (_shieldActive)
            {
                result.AppliedDamage = 0;
                result.ShouldEngageShield = true;
                return result;
            }

            if (!IsVulnerableForTurn(_lastSyncedTurn))
            {
                result.AppliedDamage = 0;
                result.ShouldEngageShield = true;
                EngageShield();
                return result;
            }

            int capRemaining = Mathf.Max(0, _maxDamageThisVulnerabilityWindow - _damageTakenThisVulnerabilityWindow);
            int applied = Mathf.Min(requestedDamage, capRemaining);
            result.AppliedDamage = applied;

            int hpAfter = currentHealth - applied;
            if (phases != null && bossMaxHealth > 0)
            {
                int phaseAfter = phases.GetPhaseIndexByHealth(hpAfter, bossMaxHealth);
                result.PhaseIndexAfterHit = phaseAfter;
                result.PhaseIndexChanged = phaseAfter >= 0 && phaseAfter != currentPhaseIndex;
            }

            bool capAlreadyExhausted = capRemaining <= 0;
            bool capFilledByThisHit = applied > 0 && applied >= capRemaining;
            result.ShouldEngageShield = capAlreadyExhausted || capFilledByThisHit;

            return result;
        }

        public static void RecordBossDamageApplied(int appliedDamage)
        {
            if (!_registered || appliedDamage <= 0) return;
            _damageTakenThisVulnerabilityWindow += appliedDamage;
        }

        public static void EngageShield()
        {
            _shieldActive = true;
            _hasVulnWindow = false;
            _damageTakenThisVulnerabilityWindow = 0;
            ApplyShieldVisual(true);
        }

        public static void DisengageShield()
        {
            _shieldActive = false;
            ApplyShieldVisual(false);
        }

        public static bool IsLakiShieldBlockingCombatInteraction() =>
            _registered && _shieldActive;

        public static bool IsShieldDownForBossPresentation() =>
            _registered && !_shieldActive;

        public static bool ShouldSuppressNewSkillSystemHighlightFor(IEffectable effectable)
        {
            if (!IsLakiShieldBlockingCombatInteraction()) return false;
            return ResolvesToBoss(effectable);
        }

        static void RecalculateMaxDamageThisVulnerabilityWindow(int bossMaxHealth)
        {
            int maxHp = Mathf.Max(1, bossMaxHealth);
            _maxDamageThisVulnerabilityWindow = Mathf.Max(1, Mathf.RoundToInt(maxHp * _maxHpFractionLossPerDiceWindow));
        }

        static bool IsVulnerableForTurn(int turn)
        {
            if (!_hasVulnWindow) return false;
            return turn >= _vulnMinTurnInclusive && turn <= _vulnMaxTurnInclusive;
        }

        static void ApplyShieldVisual(bool on)
        {
            if (_shieldRoot == null) return;
            if (_shieldRoot.activeSelf != on)
                _shieldRoot.SetActive(on);
            if (!on) return;

            var particleSystems = _shieldRoot.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                if (ps == null) continue;
                if (!ps.gameObject.activeSelf)
                    ps.gameObject.SetActive(true);
                if (!ps.isPlaying)
                    ps.Play(true);
            }
        }

        static bool ResolvesToBoss(IEffectable e)
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
