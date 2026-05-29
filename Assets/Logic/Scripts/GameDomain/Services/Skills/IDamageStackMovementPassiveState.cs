namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>Read-only snapshot of the damage-stack movement passive.</summary>
    public interface IDamageStackMovementPassiveState
    {
        bool IsEnabled { get; }

        /// <summary>Stacks accumulated since the last player turn start; consumed at the next one.</summary>
        int PendingStackCount { get; }

        /// <summary>Configured per-stack movement multiplier from the passive asset.</summary>
        float MovementRadiusMultiplierPerStack { get; }

        /// <summary>Movement multiplier that would apply if stacks were consumed now (per-stack mult ^ pending stacks).</summary>
        float PendingTurnMovementMultiplier { get; }

        /// <summary>Stacks consumed at the current player turn start (0 until the first consumption).</summary>
        int LastConsumedStackCount { get; }

        /// <summary>Movement multiplier applied this turn from consumed stacks (1 when none were consumed).</summary>
        float CurrentTurnMovementMultiplierFromStacks { get; }
    }
}
