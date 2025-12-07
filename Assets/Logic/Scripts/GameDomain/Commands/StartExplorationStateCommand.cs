using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.StartLevel;
using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.AudioService;
using System.Threading;
using UnityEngine;

public class StartExplorationStateCommand : BaseCommand, ICommandAsync {
    private INaraController _naraController;
    private ICommandFactory _commandFactory;
    private IGameInputActionsController _gameInputActionsController;
    private ICustomizeUIController _customizeUIController;

    private ExplorationInitiatorEnterData _enterData;

    public StartExplorationStateCommand SetEnterData(ExplorationInitiatorEnterData enterData) {
        _enterData = enterData;
        return this;
    }

    public override void ResolveDependencies() {
        //_gamePlayAudioClipsScriptableObject = _diContainer.Resolve<GamePlayAudioClipsScriptableObject>();
        _naraController = _diContainer.Resolve<INaraController>();
        _commandFactory = _diContainer.Resolve<ICommandFactory>();
        _gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
        _customizeUIController = _diContainer.Resolve<ICustomizeUIController>();
    }

    public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
        _gameInputActionsController.RegisterExplorationInputListeners();
        await _commandFactory.CreateCommandAsync<StartLevelCommand>().Execute(cancellationTokenSource);
        _naraController.InitEntryPointExploration();
        _customizeUIController.InitEntryPoint();
    }
}
