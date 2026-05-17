using Logic.Scripts.GameDomain.Exploration;
using Logic.Scripts.GameDomain.Exploration.QuitConfirmation;
using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.Services.CommandFactory;

public class ExitExplorationStateCommand : BaseCommand, ICommandVoid {

    private ICommandFactory _commandFactory;
    private IGameInputActionsController _gameInputActionsController;
    private IExplorationQuitConfirmationService _quitConfirmation;

    public override void ResolveDependencies() {
        _commandFactory = _diContainer.Resolve<ICommandFactory>();
        _gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
        _quitConfirmation = _diContainer.TryResolve<IExplorationQuitConfirmationService>();
    }

    public void Execute() {
        _quitConfirmation?.ForceHide();
        ExplorationInteractInputGate.Pop();
        _commandFactory.CreateCommandVoid<DisposeLevelCommand>().SetShouldReleaseAssetsFromMemory(true).Execute();
        _gameInputActionsController.UnregisterExplorationInputListeners();
        _gameInputActionsController.DisableExplorationInputs();
        _diContainer.Unbind<GameInputActionsController>();
        return;
    }
}
