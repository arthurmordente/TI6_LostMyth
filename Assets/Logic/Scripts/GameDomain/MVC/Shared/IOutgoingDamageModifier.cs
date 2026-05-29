namespace Logic.Scripts.GameDomain.MVC.Shared
{
    /// <summary>
    /// Pending multiplier applied to the next outgoing damage instance dealt by this unit (consumes on use).
    /// </summary>
    public interface IOutgoingDamageModifier
    {
        void GrantNextOutgoingDamageMultiplier(float multiplier);

        bool HasNextOutgoingDamageMultiplier { get; }

        float PendingNextOutgoingDamageMultiplier { get; }

        /// <summary>Multiplies <paramref name="multiplier"/> and clears the pending modifier. Returns false when none is active.</summary>
        bool TryConsumeNextOutgoingDamageMultiplier(ref float multiplier);
    }
}
