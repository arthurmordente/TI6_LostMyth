using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;

public class SkillHitNaraCommand : BaseCommand, ICommandVoid {

    private ICommandFactory _commandFactory;
    private IAudioService _audioService;
    private INaraController _naraController;

    private SkillHitCommandData _commandData;

    public SkillHitNaraCommand SetData(SkillHitCommandData data) {
        _commandData = data;
        return this;
    }

    public override void ResolveDependencies() {
        _commandFactory = _diContainer.Resolve<ICommandFactory>();
        _audioService = _diContainer.Resolve<IAudioService>();
        _naraController = _diContainer.Resolve<INaraController>();
    }

    public void Execute() {
        //if (_commandData != null && _commandData.AbilityData != null) {
        //    foreach (AbilityEffect effect in _commandData.AbilityData.Effects) {
        //        effect.Execute(_commandData.Caster, _commandData.Target);
        //    }
        //}
        //To-Do tocar som 
    }
}
