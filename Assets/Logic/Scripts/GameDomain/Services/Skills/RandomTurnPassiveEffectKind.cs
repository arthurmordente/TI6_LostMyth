namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>Which random buff is active for the current player turn (roulette passive).</summary>
    public enum RandomTurnPassiveEffectKind
    {
        None = 0,
        ActionPointsBonus = 1,
        MovementRadiusMultiplier = 2,
        OutgoingDamageMultiplier = 3
    }
}
