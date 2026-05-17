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
            [Range(0f, 1f), Tooltip("Laki: HP % floor for this phase. When damage would pass below it, shield engages and the next boss prepare uses this phase's Behavior.")]
            public float HealthPercentThreshold;
            public int HealthAbsoluteThreshold;
            public BossBehaviorSO Behavior;
            [Tooltip("Max fraction of max HP Laki may lose in one fight turn after shield rules apply. 0 = use BossPhases default.")]
            [Range(0f, 1f)] public float MaxHpFractionLossPerFightTurnOverride;
        }

        [SerializeField, Range(0.01f, 1f), Tooltip("Default cap on boss HP loss per fight turn (e.g. 0.33 = one third).")]
        private float _maxHpFractionLossPerFightTurn = 1f / 3f;

        [SerializeField] private PhaseEntry[] _phases;
        public PhaseEntry[] Phases => _phases;

        public float DefaultMaxHpFractionLossPerFightTurn =>
            Mathf.Clamp(_maxHpFractionLossPerFightTurn, 0.01f, 1f);

        public float GetMaxHpFractionLossPerFightTurnForPhase(int phaseIndex)
        {
            float fallback = DefaultMaxHpFractionLossPerFightTurn;
            if (_phases == null || phaseIndex < 0 || phaseIndex >= _phases.Length) return fallback;
            float o = _phases[phaseIndex].MaxHpFractionLossPerFightTurnOverride;
            return o > 0f ? Mathf.Clamp(o, 0.01f, 1f) : fallback;
        }

        public int GetPhaseIndexByHealth(int currentHealth, int maxHealth)
        {
            if (_phases == null || _phases.Length == 0) return -1;
            float hpPct = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;
            //Debug.Log($"[BossPhases] Evaluate hpPct={hpPct:0.###} (hp={currentHealth}/{maxHealth})");
            int selectedIndex = -1;
            // Para PercentBelow queremos o MENOR threshold que ainda satisfaz (mais específico)
            float bestThresholdPct = float.MaxValue;
            // Para AbsoluteBelow idem: menor valor absoluto que ainda satisfaz
            int bestAbsolute = int.MaxValue;

            for (int i = 0; i < _phases.Length; i++)
            {
                var p = _phases[i];
                switch (p.TriggerType)
                {
                    case PhaseTriggerType.HealthPercentBelow:
                        //Debug.Log($"[BossPhases] Phase[{i}] PercentBelow threshold={p.HealthPercentThreshold:0.###} behavior={(p.Behavior!=null ? p.Behavior.name : "NULL")}");
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
                        //Debug.Log($"[BossPhases] Phase[{i}] AbsoluteBelow threshold={p.HealthAbsoluteThreshold} behavior={(p.Behavior!=null ? p.Behavior.name : "NULL")}");
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
            //if (selectedIndex < 0)
            //{
            //    Debug.Log("[BossPhases] No phase matched current health.");
            //}
            return selectedIndex;
        }
    }
}


