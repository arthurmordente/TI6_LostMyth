using System;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>Stat changes applied once when the player enters combat, for <see cref="SkillType.Passive"/> skills.</summary>
    public enum PassiveStatModifierKind
    {
        None = 0,
        /// <summary>Multiplies movement arena radius (gameplay ring). Use 1.5 for +50%.</summary>
        MovementRadiusMultiplier = 1,
        /// <summary>Adds this many action points (mana) at each player turn start (after config gain).</summary>
        ActionPointsTurnGainBonus = 2
    }

    [Serializable]
    public struct PassiveStatModifierEntry
    {
        public PassiveStatModifierKind Kind;

        [Tooltip("MovementRadiusMultiplier: e.g. 1.5 = +50% ring radius. ActionPointsTurnGainBonus: integer bonus, e.g. 1.")]
        public float Value;
    }
}
