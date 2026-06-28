using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss
{
    [CreateAssetMenu(fileName = "BossPhases", menuName = "Scriptable Objects/BossPhases")]
    public class BossPhasesSO : ScriptableObject
    {
        public enum PhaseTriggerType { HealthPercentBelow, HealthAbsoluteBelow }

        [System.Serializable]
        public struct PhaseEntry
        {
            public string Name;
            public PhaseTriggerType TriggerType;
            [Range(0f, 1f), Tooltip("Laki: HP % threshold for this phase's Behavior. Does not clip damage during a dice vulnerability window.")]
            public float HealthPercentThreshold;
            public int HealthAbsoluteThreshold;
            public BossBehaviorSO Behavior;
            [Tooltip("Max fraction of max HP Laki may lose in one dice vulnerability window while in this phase. 0 = use BossPhases default.")]
            [Range(0f, 1f)] public float MaxHpFractionLossPerFightTurnOverride;
        }

        [Header("Laki — dice vulnerability window")]
        [SerializeField, Min(1), Tooltip("Fight turns with shield off after the player wins the dice minigame. 2 = turn T (dice resolves) and T+1.")]
        private int _vulnerabilityFightTurnCount = 2;

        [SerializeField, Range(0.01f, 1f), Tooltip("Default cap on boss HP loss across the entire dice vulnerability window (e.g. 0.33 = one third of max HP total).")]
        private float _maxHpFractionLossPerFightTurn = 1f / 3f;

        [SerializeField] private PhaseEntry[] _phases;
        public PhaseEntry[] Phases => _phases;

        public int VulnerabilityFightTurnCount => Mathf.Max(1, _vulnerabilityFightTurnCount);

        public float DefaultMaxHpFractionLossPerDiceWindow =>
            Mathf.Clamp(_maxHpFractionLossPerFightTurn, 0.01f, 1f);

        public float DefaultMaxHpFractionLossPerFightTurn => DefaultMaxHpFractionLossPerDiceWindow;

        public float GetMaxHpFractionLossPerDiceWindowForPhase(int phaseIndex)
        {
            float fallback = DefaultMaxHpFractionLossPerDiceWindow;
            if (_phases == null || phaseIndex < 0 || phaseIndex >= _phases.Length) return fallback;
            float o = _phases[phaseIndex].MaxHpFractionLossPerFightTurnOverride;
            return o > 0f ? Mathf.Clamp(o, 0.01f, 1f) : fallback;
        }

        public float GetMaxHpFractionLossPerFightTurnForPhase(int phaseIndex) =>
            GetMaxHpFractionLossPerDiceWindowForPhase(phaseIndex);

        public int GetPhaseIndexByHealth(int currentHealth, int maxHealth)
        {
            if (_phases == null || _phases.Length == 0) return -1;
            float hpPct = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;
            int selectedIndex = -1;
            float bestThresholdPct = float.MaxValue;
            int bestAbsolute = int.MaxValue;

            for (int i = 0; i < _phases.Length; i++)
            {
                var p = _phases[i];
                switch (p.TriggerType)
                {
                    case PhaseTriggerType.HealthPercentBelow:
                        if (hpPct <= p.HealthPercentThreshold && p.Behavior != null)
                        {
                            if (p.HealthPercentThreshold < bestThresholdPct)
                            {
                                bestThresholdPct = p.HealthPercentThreshold;
                                selectedIndex = i;
                            }
                        }
                        break;
                    case PhaseTriggerType.HealthAbsoluteBelow:
                        if (currentHealth <= p.HealthAbsoluteThreshold && p.Behavior != null)
                        {
                            if (p.HealthAbsoluteThreshold < bestAbsolute)
                            {
                                bestAbsolute = p.HealthAbsoluteThreshold;
                                selectedIndex = i;
                            }
                        }
                        break;
                }
            }
            return selectedIndex;
        }
    }
}
