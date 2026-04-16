namespace Logic.Scripts.GameDomain.MVC.Boss.Visuals
{
    /// <summary>
    /// Row key in <see cref="CombatAttackVisualCatalogSO"/> (Hokari fight). Values 1–14 are stable for serialized prefabs.
    /// Multiple ids can share the same catalog row (e.g. reuse <see cref="WingSlash"/> for <see cref="BigWindSlash"/>).
    /// </summary>
    public enum HokariBossAttackVisualId
    {
        None = 0,
        BigCones = 1,
        BigWindSlash = 2,
        XFeatherG = 3,
        XFeatherK = 4,
        ZFeatherG = 5,
        ZFeatherK = 6,
        XZFeatherG = 7,
        XZFeatherK = 8,
        SkySwordsG = 9,
        SkySwordsK = 10,
        BigSkySwordsG = 11,
        BigSkySwordsK = 12,
        Orb = 13,
        BigOrb = 14,
        ProteanCones = 15,
        WingSlash = 16,
        SkySwords = 17,
        Circle = 18,
    }
}
