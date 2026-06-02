using Logic.Scripts.GameDomain.MVC.Nara;
using Zenject;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public sealed class SkillCastBeneficiaryResolver : ISkillCastBeneficiaryResolver, IInitializable
    {
        static ISkillCastBeneficiaryResolver _instance;

        readonly INaraController _nara;

        public SkillCastBeneficiaryResolver(INaraController nara)
        {
            _nara = nara;
        }

        public void Initialize()
        {
            _instance = this;
        }

        public IEffectable Resolve(SkillDataSO skill, IEffectable caster)
        {
            if (SkillCastRules.IsRangelessSelfBuff(skill) && _nara != null)
                return _nara;
            return caster;
        }

        public static IEffectable TryResolve(SkillDataSO skill, IEffectable caster)
        {
            if (_instance != null)
                return _instance.Resolve(skill, caster);
            return caster;
        }
    }
}
