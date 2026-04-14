using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;

public interface ISkillCastFlow
{
    bool CanHandleCaster(IPlayableUnit caster);
    void InitEntryPoint(INaraController naraController);
    bool TryPrepareCast(int index, IPlayableUnit caster, out SkillCastPrepareResult prepareResult);
    void ExecutePreparedCast(IPlayableUnit caster);
    void CancelPreparedCast(IPlayableUnit caster);
}

public struct SkillCastPrepareResult
{
    public int AbilityIndex;
    public int Cost;
    public int AnimatorAttackType;
}
