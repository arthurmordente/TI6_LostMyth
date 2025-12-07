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
        _inputActionsController = _diContainer.Resolve<IGameInputActionsController>();
    }

    public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
        _levelCancellationTokenService.InitCancellationToken();
        //To-Do pregame da Ui
        int levelNumber = _commandData.LevelNumber;
        _gamePlayDataService.SetCurrentLevelNumber(levelNumber);
        await CreateLevelScenario(levelNumber, cancellationTokenSource);
        NaraMovementController movementController = _naraMovementControllerFactory.Create(_levelsDataService.GetLevelData(levelNumber).ControllerType, _levelsDataService.GetLevelData(levelNumber).NaraLevelConfiguration);
        _naraController.CreateNara(movementController);
        _naraController.Freeeze();
        // Set initial player position from LevelTurnData -> BossConfiguration (before InitEntryPointGamePlay runs)
        var levelTurnData = _levelsDataService.GetLevelData(levelNumber) as LevelTurnData;
        if (levelTurnData != null && levelTurnData.BossConfiguration != null) {
            _naraController.SetPosition(levelTurnData.BossConfiguration.InitialPlayerPosition);
        }
    }
    private async Awaitable CreateLevelScenario(int levelNumber, CancellationTokenSource cancellationTokenSource) {
        await _levelScenarioController.CreateLevelScenario(levelNumber, cancellationTokenSource);
        _portalController.SetUpPortals(_levelScenarioController.CurrentLevelScenarioView.PortalViews);
        _interactableObjectsController.SetUpInteractables(_levelScenarioController.CurrentLevelScenarioView.Interactableviews);
        //To-Do adicionar efeitos do cenario
    }

    public LoadLevelCommand SetBoss(int levelNumber) {
        LevelTurnData levelTurnData = (LevelTurnData)_levelsDataService.GetLevelData(levelNumber);
        _diContainer.BindInstance(levelTurnData.BossPhases);
        _diContainer.BindInterfacesTo<BossAbilityController>().AsSingle().WithArguments((BossBehaviorSO)null).NonLazy();
        _diContainer.BindInterfacesTo<BossController>().AsSingle().WithArguments(levelTurnData.BossPrefab, levelTurnData.BossConfiguration, levelTurnData.BossPhases).NonLazy();
        _diContainer.BindInterfacesAndSelfTo<BossActionService>().AsSingle().NonLazy();
        return this;
    }
}
