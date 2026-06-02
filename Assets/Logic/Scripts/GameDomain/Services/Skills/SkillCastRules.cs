namespace Logic.Scripts.GameDomain.Services.Skills
{
    public static class SkillCastRules
    {
        public static bool IsRangelessSelfBuff(SkillDataSO skill) =>
            skill != null && skill.SkillType == SkillType.SelfBuff;
    }
}
