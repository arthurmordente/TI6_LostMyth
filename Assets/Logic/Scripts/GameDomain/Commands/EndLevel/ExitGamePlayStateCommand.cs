using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem;
using Logic.Scripts.Services.CommandFactory;

public class ExitGamePlayStateCommand : BaseCommand, ICommandVoid {

    private ICommandFactory _commandFactory;
    private IGameInputActionsController _gameInputActionsController;

    public override void ResolveDependencies() {
        _commandFactory = _diContainer.Resolve<ICommandFactory>();
        _gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
    }

    public void Execute() {
        _diContainer.TryResolve<ICastController>()?.CancelAbilityUse();
        _diContainer.TryResolve<INewSkillSystemSkillTargetingPreviewService>()?.End();
        _commandFactory.CreateCommandVoid<DisposeLevelCommand>().SetShouldReleaseAssetsFromMemory(true).Execute();
        _gameInputActionsController.UnregisterGameplayInputListeners();
        _gameInputActionsController.DisableGameplayInputs();
        _diContainer.Unbind<GameInputActionsController>();
        return;
    }
}
