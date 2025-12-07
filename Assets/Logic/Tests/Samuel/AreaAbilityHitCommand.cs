using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.Services.CommandFactory;

public class AreaAbilityHitCommand : BaseCommand, ICommandVoid
{
    private SkillHitCommandData _commandData;

    public AreaAbilityHitCommand SetData(SkillHitCommandData data)
    {
        _commandData = data;
        return this;
    }

    public override void ResolveDependencies()
    {
        /*_commandFactory = _diContainer.Resolve<ICommandFactory>();
        _audioService = _diContainer.Resolve<IAudioService>();
        _naraController = _diContainer.Resolve<INaraController>();*/
    }

    public void Execute()
    {
        //if (_commandData != null)
        //{
        //    foreach (AbilityEffect effect in _commandData.AbilityData.Effects)
        //    {
        //        effect.Execute(_commandData.Caster, _commandData.Target);
        //    }
        //}
    }
}
