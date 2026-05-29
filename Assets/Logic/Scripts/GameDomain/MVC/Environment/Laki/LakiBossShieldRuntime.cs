using Assets.Logic.Scripts.GameDomain.Effects;
using Logic.Scripts.GameDomain.MVC.Boss;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>
    /// Laki arena: shield VFX and damage gate stay in sync via <see cref="EngageShield"/> / <see cref="DisengageShield"/>.
    /// Phase <see cref="BossPhasesSO.PhaseEntry.HealthPercentThreshold"/> values are HP floors — crossing one engages the shield
    /// and blocks further damage for the rest of that fight turn (dice vulnerable window only).
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
        private static int _lastSyncedTurn = -1;
        private static int _damageTakenThisFightTurn;
        private static int _maxDamageThisFightTurn;
        private static float _maxHpFractionLossPerFightTurn = 1f / 3f;

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
                _maxHpFractionLossPerFightTurn = phases.GetMaxHpFractionLossPerFightTurnForPhase(currentPhaseIndex);
            RecalculateMaxDamageThisFightTurn(bossMaxHealth);
        }

        public static void Reset()
        {
            _shieldRoot = null;
            _registered = false;
            _shieldActive = false;
            _hasVulnWindow = false;
            _vulnMinTurnInclusive = 0;
            _vulnMaxTurnInclusive = 0;
            _lastSyncedTurn = -1;
            _damageTakenThisFightTurn = 0;
            _maxDamageThisFightTurn = 0;
            _maxHpFractionLossPerFightTurn = 1f / 3f;
        }

        public static void SyncFightTurn(int fightTurnNumber)
        {
            _lastSyncedTurn = fightTurnNumber;
            _damageTakenThisFightTurn = 0;
            if (IsVulnerableForTurn(fightTurnNumber))
                DisengageShield();
            else
                EngageShield();
        }

        public static void RegisterDicePlayerWin(int resolvedDuringFightTurn)
        {
            _hasVulnWindow = true;
            _vulnMinTurnInclusive = resolvedDuringFightTurn;
            _vulnMaxTurnInclusive = resolvedDuringFightTurn + 1;
            _lastSyncedTurn = resolvedDuringFightTurn;
            _damageTakenThisFightTurn = 0;
            DisengageShield();
        }

        /// <summary>Phase transition — always pairs mechanical block with shield VFX.</summary>
        public static void NotifyBossPhaseChanged(BossPhasesSO phases, int newPhaseIndex, int bossMaxHealth)
        {
            if (!_registered) return;
            _hasVulnWindow = false;
            if (phases != null)
                _maxHpFractionLossPerFightTurn = phases.GetMaxHpFractionLossPerFightTurnForPhase(newPhaseIndex);
            RecalculateMaxDamageThisFightTurn(bossMaxHealth);
            EngageShield();
            Debug.Log(
                $"[LakiShield] Phase {newPhaseIndex} — shield ON. Turn damage={_damageTakenThisFightTurn}/{_maxDamageThisFightTurn} " +
                $"(cap={_maxHpFractionLossPerFightTurn:P0} max HP).");
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
                RecalculateMaxDamageThisFightTurn(bossMaxHealth);

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

            int capRemaining = Mathf.Max(0, _maxDamageThisFightTurn - _damageTakenThisFightTurn);
            int floorHp = GetNextPhaseFloorHp(bossMaxHealth, phases, currentPhaseIndex);
            int maxByFloor = floorHp > 0 ? Mathf.Max(0, currentHealth - floorHp) : currentHealth;
            int applied = Mathf.Min(requestedDamage, capRemaining, maxByFloor);
            result.AppliedDamage = applied;

            int hpAfter = currentHealth - applied;
            if (phases != null && bossMaxHealth > 0)
            {
                int phaseAfter = phases.GetPhaseIndexByHealth(hpAfter, bossMaxHealth);
                result.PhaseIndexAfterHit = phaseAfter;
                result.PhaseIndexChanged = phaseAfter >= 0 && phaseAfter != currentPhaseIndex;
            }

            bool capHit = capRemaining <= 0 || (applied > 0 && applied >= capRemaining && applied < requestedDamage);
            bool floorHit = floorHp > 0 && (hpAfter <= floorHp || (applied > 0 && applied >= maxByFloor && maxByFloor < requestedDamage));
            result.ShouldEngageShield = capHit || floorHit || result.PhaseIndexChanged;

            return result;
        }

        public static void RecordBossDamageApplied(int appliedDamage)
        {
            if (!_registered || appliedDamage <= 0) return;
            _damageTakenThisFightTurn += appliedDamage;
        }

        public static void EngageShield()
        {
            _shieldActive = true;
            _hasVulnWindow = false;
            ApplyShieldVisual(true);
        }

        public static void DisengageShield()
        {
            _shieldActive = false;
            ApplyShieldVisual(false);
        }

        public static bool IsLakiShieldBlockingCombatInteraction() =>
            _registered && _shieldActive;

        /// <summary>Escudo Laki registado e desligado (janela de vulnerabilidade / escudo caído).</summary>
        public static bool IsShieldDownForBossPresentation() =>
            _registered && !_shieldActive;

        public static bool ShouldSuppressNewSkillSystemHighlightFor(IEffectable effectable)
        {
            if (!IsLakiShieldBlockingCombatInteraction()) return false;
            return ResolvesToBoss(effectable);
        }

        /// <summary>Minimum HP allowed while in <paramref name="currentPhaseIndex"/> before the next phase threshold (percent on BossPhases).</summary>
        public static int GetNextPhaseFloorHp(int bossMaxHealth, BossPhasesSO phases, int currentPhaseIndex)
        {
            if (phases == null || bossMaxHealth <= 0) return 0;
            int nextPhase = currentPhaseIndex + 1;
            var entries = phases.Phases;
            if (entries == null || nextPhase < 0 || nextPhase >= entries.Length) return 0;
            var entry = entries[nextPhase];
            if (entry.TriggerType != BossPhasesSO.PhaseTriggerType.HealthPercentBelow) return 0;
            float t = Mathf.Clamp01(entry.HealthPercentThreshold);
            if (t <= 0f) return 0;
            return Mathf.CeilToInt(bossMaxHealth * t);
        }

        static void RecalculateMaxDamageThisFightTurn(int bossMaxHealth)
        {
            int maxHp = Mathf.Max(1, bossMaxHealth);
            _maxDamageThisFightTurn = Mathf.Max(1, Mathf.RoundToInt(maxHp * _maxHpFractionLossPerFightTurn));
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
