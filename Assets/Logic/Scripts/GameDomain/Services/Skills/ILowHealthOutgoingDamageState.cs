namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>Read-only snapshot of the low-health outgoing damage passive.</summary>
    public interface ILowHealthOutgoingDamageState
    {
        bool IsEnabled { get; }

        /// <summary>Current outgoing damage multiplier from missing HP (max 2 decimal places).</summary>
        float CurrentMultiplier { get; }
    }
}
