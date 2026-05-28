using Logic.Scripts.Core.CoreInitiator.Base;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.InitiatorInvokerService;
using Logic.Scripts.Utils;
using System.Threading;
using UnityEngine;
using Zenject;

public class LobbyInitiator : ISceneInitiator, ILobbyInitiator {
    private readonly ICommandFactory _commandFactory;
    private readonly ISceneInitiatorsService _sceneInitiatorsService;
    public SceneType SceneType => SceneType.LobbyScene;

    [Inject] ILobbyController _controller;

    public LobbyInitiator(ICommandFactory commandFactory, ISceneInitiatorsService sceneInitiatorsService) {
        _sceneInitiatorsService = sceneInitiatorsService;
        _commandFactory = commandFactory;
        _sceneInitiatorsService.RegisterInitiator(this);
    }

    public Awaitable InitExitPoint(CancellationTokenSource cancellationTokenSource) {
        _sceneInitiatorsService.UnregisterInitiator(this);
        _controller.HideMenu();
        return AwaitableUtils.CompletedTask;
    }

    public Awaitable LoadEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
        //To-Do Adicionar comando de entrada do lobby
        _controller.InitEntryPoint();
        return AwaitableUtils.CompletedTask;
    }

    public Awaitable StartEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
        return AwaitableUtils.CompletedTask;
    }
}
