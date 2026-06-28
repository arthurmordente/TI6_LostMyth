using System;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(fileName = "RandomTurnPassiveBehavior", menuName = "ScriptableObjects/Skills/Passive/Random Turn Roulette")]
public class RandomTurnPassiveBehaviorSO : PassiveTurnBehaviorSO
{
    [Serializable]
    public struct Entry
    {
        public RandomTurnPassiveEffectKind Kind;
        [Tooltip("ActionPointsBonus: integer bonus. Multipliers: e.g. 1.2 = +20%.")]
        public float Value;
        [Tooltip("Relative weight for this entry. Probabilities are Weight / sum(all weights).")]
        [Min(0)] public int Weight;
        [TextArea] public string DisplayText;
    }

    [SerializeField] private Entry[] _pool =
    {
        new Entry { Kind = RandomTurnPassiveEffectKind.ActionPointsBonus, Value = 1f, Weight = 1 },
        new Entry { Kind = RandomTurnPassiveEffectKind.MovementRadiusMultiplier, Value = 1.2f, Weight = 1 },
        new Entry { Kind = RandomTurnPassiveEffectKind.OutgoingDamageMultiplier, Value = 1.2f, Weight = 1 }
    };

    public int TotalWeight
    {
        get
        {
            int total = 0;
            if (_pool == null) return 0;
            for (int i = 0; i < _pool.Length; i++)
                total += Mathf.Max(0, _pool[i].Weight);
            return total;
        }
    }

    public float GetEntryProbability(int index)
    {
        if (_pool == null || index < 0 || index >= _pool.Length) return 0f;
        int total = TotalWeight;
        if (total <= 0) return 0f;
        return Mathf.Max(0, _pool[index].Weight) / (float)total;
    }

    public override bool TryRollTurnEffect(
        out RandomTurnPassiveEffectKind kind,
        out float value,
        out string displayText)
    {
        kind = RandomTurnPassiveEffectKind.None;
        value = 0f;
        displayText = string.Empty;
        if (_pool == null || _pool.Length == 0) return false;

        int totalWeight = TotalWeight;
        int index;
        if (totalWeight <= 0)
            index = UnityEngine.Random.Range(0, _pool.Length);
        else
            index = RollWeightedIndex(totalWeight);

        kind = _pool[index].Kind;
        value = _pool[index].Value;
        displayText = _pool[index].DisplayText ?? string.Empty;
        return kind != RandomTurnPassiveEffectKind.None;
    }

    /// <summary>
    /// Picks an index in [0, pool.Length) using integer weights.
    /// roll is in [0, totalWeight); first entry whose cumulative weight exceeds roll wins.
    /// </summary>
    int RollWeightedIndex(int totalWeight)
    {
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < _pool.Length; i++)
        {
            cumulative += Mathf.Max(0, _pool[i].Weight);
            if (roll < cumulative)
                return i;
        }

        return _pool.Length - 1;
    }

    private void OnValidate()
    {
        if (_pool == null || _pool.Length == 0)
        {
            _pool = new[]
            {
                new Entry { Kind = RandomTurnPassiveEffectKind.ActionPointsBonus, Value = 1f, Weight = 1 },
                new Entry { Kind = RandomTurnPassiveEffectKind.MovementRadiusMultiplier, Value = 1.2f, Weight = 1 },
                new Entry { Kind = RandomTurnPassiveEffectKind.OutgoingDamageMultiplier, Value = 1.2f, Weight = 1 }
            };
            return;
        }

        bool anyPositiveWeight = false;
        for (int i = 0; i < _pool.Length; i++)
        {
            if (_pool[i].Weight < 0)
                _pool[i].Weight = 0;
            if (_pool[i].Weight > 0)
                anyPositiveWeight = true;
        }

        if (!anyPositiveWeight)
        {
            for (int i = 0; i < _pool.Length; i++)
                _pool[i].Weight = 1;
        }
    }
}
