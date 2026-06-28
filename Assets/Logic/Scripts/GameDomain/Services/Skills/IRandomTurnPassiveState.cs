using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>Read-only snapshot of the roulette passive for UI / gameplay queries.</summary>
    public interface IRandomTurnPassiveState
    {
        /// <summary>True when a loadout passive references a <see cref="PassiveTurnBehaviorSO"/> roulette behavior.</summary>
        bool IsEnabled { get; }

        /// <summary>Effect rolled for the current player turn; <see cref="RandomTurnPassiveEffectKind.None"/> before the first roll or when disabled.</summary>
        RandomTurnPassiveEffectKind ActiveEffect { get; }

        /// <summary>Value tied to <see cref="ActiveEffect"/> (e.g. +1 AP, 1.2x movement, 1.2x damage).</summary>
        float ActiveEffectValue { get; }

        /// <summary>Outgoing damage multiplier from the turn passive this turn (1 when another effect is active).</summary>
        float TurnOutgoingDamageMultiplier { get; }

        /// <summary>Display text for the entry rolled this turn; empty before the first roll or when disabled.</summary>
        string ActiveRollDisplayText { get; }

        /// <summary>Passive skill asset driving the roulette; null when disabled.</summary>
        SkillDataSO ActivePassiveSkill { get; }
    }
}
