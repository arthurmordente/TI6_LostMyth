using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Boss;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Turns;
using System.Threading;
using UnityEngine;

public class LoadLevelCommand : BaseCommand, ICommandAsync {

    private ILevelScenarioController _levelScenarioController;
    private INaraController _naraController;
    private ILevelCancellationTokenService _levelCancellationTokenService;
    private ILevelsDataService _levelsDataService;
    private INaraMovementControllerFactory _naraMovementControllerFactory;
    private IGamePlayDataService _gamePlayDataService;
    private IPortalController _portalController;
    private IInteractableObjectsController _interactableObjectsController;
    private ILobbyInteractionZoneController _lobbyInteractionZoneController;
    private IGameInputActionsController _inputActionsController;

    //To-Do adicionar efeitos do cenario

    private LoadLevelCommandData _commandData;

    public LoadLevelCommand SetEnterData(LoadLevelCommandData commandData) {
        _commandData = commandData;
        return this;
    }

    public override void ResolveDependencies() {
        _levelScenarioController = _diContainer.Resolve<ILevelScenarioController>();
        _naraController = _diContainer.Resolve<INaraController>();
        _levelCancellationTokenService = _diContainer.Resolve<ILevelCancellationTokenService>();
        _levelsDataService = _diContainer.Resolve<ILevelsDataService>();
        _gamePlayDataService = _diContainer.Resolve<IGamePlayDataService>();
        _naraMovementControllerFactory = _diContainer.Resolve<INaraMovementControllerFactory>();
        _portalController = _diContainer.Resolve<IPortalController>();
        _interactableObjectsController = _diContainer.Resolve<IInteractableObjectsController>();
        _lobbyInteractionZoneController = _diContainer.TryResolve<ILobbyInteractionZoneController>();
        _inputActionsController = _diContainer.Resolve<IGameInputActionsController>();
    }

    public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
        GameOverCommand.ResetSequenceGuard();
        Time.timeScale = 1f;
        _levelCancellationTokenService.InitCancellationToken();
        //To-Do pregame da Ui
        int levelNumber = _commandData.LevelNumber;
        _gamePlayDataService.SetCurrentLevelNumber(levelNumber);
        await CreateLevelScenario(levelNumber, cancellationTokenSource);
        NaraMovementController movementController = _naraMovementControllerFactory.Create(_levelsDataService.GetLevelData(levelNumber).ControllerType, _levelsDataService.GetLevelData(levelNumber).NaraLevelConfiguration);
        _naraController.CreateNara(movementController);
        _naraController.Freeeze();
        // Set initial player position before movement init (turn fights vs exploration lobby).
        var levelData = _levelsDataService.GetLevelData(levelNumber);
        if (levelData is LevelTurnData levelTurnData && levelTurnData.BossConfiguration != null) {
            _naraController.SetPosition(levelTurnData.BossConfiguration.InitialPlayerPosition);
        } else if (levelData is LevelExplorationData explorationData) {
            _naraController.SetPosition(explorationData.InitialPlayerPosition);
        }
    }
    private async Awaitable CreateLevelScenario(int levelNumber, CancellationTokenSource cancellationTokenSource) {
        await _levelScenarioController.CreateLevelScenario(levelNumber, cancellationTokenSource);
        var scenarioView = _levelScenarioController.CurrentLevelScenarioView;
        _portalController.SetUpPortals(scenarioView.PortalViews);
        _interactableObjectsController.SetUpInteractables(scenarioView.Interactableviews);
        _lobbyInteractionZoneController?.Clear();
        if (levelNumber == 0)
        {
            var zones = LobbyInteractionZoneBootstrap.EnsureZones(
                scenarioView.transform,
                scenarioView.LobbyInteractionZones);
            _lobbyInteractionZoneController?.Setup(zones);
        }
        //To-Do adicionar efeitos do cenario
    }

    public LoadLevelCommand SetBoss(int levelNumber) {
        LevelTurnData levelTurnData = (LevelTurnData)_levelsDataService.GetLevelData(levelNumber);
        _diContainer.BindInstance(levelTurnData.BossPhases);
        _diContainer.BindInterfacesTo<BossAbilityController>().AsSingle().WithArguments((BossBehaviorSO)null).NonLazy();
        _diContainer.BindInterfacesTo<BossController>().AsSingle()
            .WithArguments(levelTurnData.BossPrefab, levelTurnData.BossConfiguration, levelTurnData.BossPhases, levelTurnData.GetEffectiveBossHudDisplayName())
            .NonLazy();
        _diContainer.BindInterfacesAndSelfTo<BossActionService>().AsSingle().NonLazy();
        return this;
    }
}
