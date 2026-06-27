using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.CommandFactory;

public class InteractInputCommand : BaseCommand, ICommandVoid {
    private IInteractableObjectsController _interactableObjectsController;
    private INaraController _naraController;
    private IGamePlayDataService _gamePlayDataService;
    private ILobbyInteractionZoneController _lobbyInteractionZoneController;
    private ICommandFactory _commandFactory;

    public override void ResolveDependencies() {
        _interactableObjectsController = _diContainer.Resolve<IInteractableObjectsController>();
        _naraController = _diContainer.Resolve<INaraController>();
        _gamePlayDataService = _diContainer.Resolve<IGamePlayDataService>();
        _lobbyInteractionZoneController = _diContainer.TryResolve<ILobbyInteractionZoneController>();
        _commandFactory = _diContainer.Resolve<ICommandFactory>();
    }

    public void Execute() {
        if (_gamePlayDataService.CurrentLevelNumber == 0 && TryHandleLobbyZoneInteraction())
            return;

        _interactableObjectsController.VerifyInteractables(_naraController);
    }

    bool TryHandleLobbyZoneInteraction() {
        if (_lobbyInteractionZoneController == null)
            return false;

        var activeZone = _lobbyInteractionZoneController.GetActiveZone();
        if (activeZone == null)
            return false;

        switch (activeZone.Kind) {
            case LobbyInteractionKind.TipsCatalog:
                _commandFactory.CreateCommandVoid<OnLobbyTipsInteractionCommand>().Execute();
                return true;
            case LobbyInteractionKind.SkillLoadout:
                _commandFactory.CreateCommandVoid<OnSkillLoadoutInteractionCommand>().Execute();
                return true;
            default:
                return false;
        }
    }
}
