using Logic.Scripts.Core.CoreInitiator.Base;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.InitiatorInvokerService;
using System.Threading;
using UnityEngine;

public class ExplorationInitiator : ISceneInitiator, IExplorationInitiator {
    private readonly ICommandFactory _commandFactory;
    private readonly ISceneInitiatorsService _sceneInitiatorsService;

    public SceneType SceneType => SceneType.ExplorationScene;

    public ExplorationInitiator(ICommandFactory commandFactory, ISceneInitiatorsService sceneInitiatorsService) {
        _commandFactory = commandFactory;
        _sceneInitiatorsService = sceneInitiatorsService;
        _sceneInitiatorsService.RegisterInitiator(this);
    }

    public async Awaitable LoadEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
        var enterData = (ExplorationInitiatorEnterData)enterDataObject;
        await _commandFactory.CreateCommandAsync<LoadExplorationStateCommand>().SetEnterData(enterData).Execute(cancellationTokenSource);
    }

    public async Awaitable StartEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
        var enterData = (ExplorationInitiatorEnterData)enterDataObject;
        await _commandFactory.CreateCommandAsync<StartExplorationStateCommand>().SetEnterData(enterData).Execute(cancellationTokenSource);
    }

    public async Awaitable InitExitPoint(CancellationTokenSource cancellationTokenSource) {
        await Awaitable.NextFrameAsync(cancellationTokenSource.Token);
        _sceneInitiatorsService.UnregisterInitiator(this);
        _commandFactory.CreateCommandVoid<ExitExplorationStateCommand>().Execute();
    }
}
