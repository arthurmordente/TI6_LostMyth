using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.StateMachineService;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Utils;
using System.Threading;
using UnityEngine;

public class PortalEnterCommand : BaseCommand, ICommandAsync {
    private IStateMachineService _stateMachineService;
    private GamePlayState.Factory _gameplayStateFactory;
    private IAudioService _audioService;

    private PortalEnterCommandData _portalEnterCommandData;

    public PortalEnterCommand SetData(PortalEnterCommandData portalEnterCommandData) {
        _portalEnterCommandData = portalEnterCommandData;
        return this;
    }

    public override void ResolveDependencies() {
        _stateMachineService = _diContainer.Resolve<IStateMachineService>();
        _gameplayStateFactory = _diContainer.Resolve<GamePlayState.Factory>();
        try { _audioService = _diContainer.Resolve<IAudioService>(); } catch { _audioService = null; }
    }

    public Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
        GeneralSfxFeedback.PlayPortal(_audioService);
        int levelIndex = _portalEnterCommandData != null ? _portalEnterCommandData.LevelToEnter : 1;
        _stateMachineService.SwitchState(_gameplayStateFactory.Create(new GamePlayInitatorEnterData(levelIndex)));
        return AwaitableUtils.CompletedTask;
    }

}
