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
    }

    [SerializeField] private Entry[] _pool =
    {
        new Entry { Kind = RandomTurnPassiveEffectKind.ActionPointsBonus, Value = 1f },
        new Entry { Kind = RandomTurnPassiveEffectKind.MovementRadiusMultiplier, Value = 1.2f },
        new Entry { Kind = RandomTurnPassiveEffectKind.OutgoingDamageMultiplier, Value = 1.2f }
    };

    public override bool TryRollTurnEffect(out RandomTurnPassiveEffectKind kind, out float value)
    {
        kind = RandomTurnPassiveEffectKind.None;
        value = 0f;
        if (_pool == null || _pool.Length == 0) return false;

        int index = UnityEngine.Random.Range(0, _pool.Length);
        kind = _pool[index].Kind;
        value = _pool[index].Value;
        return kind != RandomTurnPassiveEffectKind.None;
    }

    private void OnValidate()
    {
        if (_pool == null || _pool.Length == 0)
        {
            _pool = new[]
            {
                new Entry { Kind = RandomTurnPassiveEffectKind.ActionPointsBonus, Value = 1f },
                new Entry { Kind = RandomTurnPassiveEffectKind.MovementRadiusMultiplier, Value = 1.2f },
                new Entry { Kind = RandomTurnPassiveEffectKind.OutgoingDamageMultiplier, Value = 1.2f }
            };
        }
    }
}
