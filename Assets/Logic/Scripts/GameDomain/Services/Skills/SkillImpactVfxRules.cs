using Logic.Scripts.GameDomain.MVC.Environment.Laki;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public static class SkillImpactVfxRules
    {
        public static bool ShouldSpawnImpactOn(IEffectable hit)
        {
            if (hit == null) return false;
            if (LakiBossShieldRuntime.ShouldSuppressNewSkillSystemHighlightFor(hit))
                return false;
            return true;
        }
    }
}
