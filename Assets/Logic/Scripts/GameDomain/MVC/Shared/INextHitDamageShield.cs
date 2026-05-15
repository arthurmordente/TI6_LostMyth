namespace Logic.Scripts.GameDomain.MVC.Shared
{
    /// <summary>
    /// Next incoming damage hit is fully absorbed (no HP loss). Used by self-buff shield skills.
    /// </summary>
    public interface INextHitDamageShield
    {
        void GrantNextHitShield();

        /// <summary>True if the next positive <see cref="IEffectable.TakeDamage"/> would be fully absorbed.</summary>
        bool HasNextHitShieldActive { get; }
    }
}
