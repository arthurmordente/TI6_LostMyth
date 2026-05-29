namespace Logic.Scripts.GameDomain.MVC.Shared
{
    /// <summary>Outgoing skill damage heals the caster for a fraction of damage dealt.</summary>
    public interface IOutgoingDamageLifesteal
    {
        float OutgoingLifestealPercent { get; }

        void SetOutgoingLifestealPercent(float percentOfDamageDealt);
    }
}
