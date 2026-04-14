using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.UpdateService;

public class LegacySkillCastFlow : ISkillCastFlow
{
    private readonly IUpdateSubscriptionService _subscriptionService;
    private readonly ICommandFactory _commandFactory;

    private AbilityData _currentAbility;

    public LegacySkillCastFlow(IUpdateSubscriptionService subscriptionService, ICommandFactory commandFactory)
    {
        _subscriptionService = subscriptionService;
        _commandFactory = commandFactory;
    }

    public bool CanHandleCaster(IPlayableUnit caster)
    {
        if (caster == null || caster.UnitViewGO == null) return false;
        var toggle = caster.UnitViewGO.GetComponent<LegacySkillSystemToggle>();
        return toggle != null && toggle.UseLegacySkillSystem;
    }

    public void InitEntryPoint(INaraController naraController)
    {
        if (naraController == null) return;

        foreach (AbilityData ability in naraController.GetAbilities())
        {
            if (ability != null) ability.SetUp(_subscriptionService, _commandFactory);
        }
    }

    public bool TryPrepareCast(int index, IPlayableUnit caster, out SkillCastPrepareResult prepareResult)
    {
        prepareResult = default;
        var abilities = caster?.GetAbilities();
        if (abilities == null) return false;
        if (index < 0 || index >= abilities.Length) return false;
        if (abilities[index] == null) return false;

        _currentAbility = abilities[index];
        _currentAbility.Aim(caster);

        prepareResult = new SkillCastPrepareResult
        {
            AbilityIndex = index,
            Cost = _currentAbility.GetCost(),
            AnimatorAttackType = _currentAbility.AnimatorAttackType
        };
        return true;
    }

    public void ExecutePreparedCast(IPlayableUnit caster)
    {
        _currentAbility?.Cast(caster);
    }

    public void CancelPreparedCast(IPlayableUnit caster)
    {
        _currentAbility?.Cancel();
        _currentAbility = null;
    }
}
