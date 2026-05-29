using Logic.Scripts.GameDomain.MVC.Shared;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public static class OutgoingDamageLifestealRuntime
    {
        public static void ClearForCaster(IEffectable caster)
        {
            if (caster is IOutgoingDamageLifesteal lifesteal)
                lifesteal.SetOutgoingLifestealPercent(0f);
        }
    }
}
