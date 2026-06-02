namespace Logic.Scripts.GameDomain.Services.Skills
{
    public interface ISkillCastBeneficiaryResolver
    {
        IEffectable Resolve(SkillDataSO skill, IEffectable caster);
    }
}
