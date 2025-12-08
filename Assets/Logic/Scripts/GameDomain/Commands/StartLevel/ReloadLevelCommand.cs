using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.StartLevel;
using Logic.Scripts.Services.CommandFactory;
using System.Threading;
using UnityEngine;

public class ReloadLevelCommand : BaseCommand, ICommandAsync {
    private ICommandFactory _commandFactory;
    private IGamePlayDataService _gamePlayDataService;

    public override void ResolveDependencies() {
        _commandFactory = _diContainer.Resolve<ICommandFactory>();
        _gamePlayDataService = _diContainer.Resolve<IGamePlayDataService>();
    }

    public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
        _commandFactory.CreateCommandVoid<DisposeLevelCommand>().Execute();
        await _commandFactory.CreateCommandAsync<LoadLevelCommand>().SetEnterData(new LoadLevelCommandData(_gamePlayDataService.CurrentLevelNumber)).Execute(cancellationTokenSource);
        await _commandFactory.CreateCommandAsync<StartLevelCommand>().Execute(cancellationTokenSource);
    }
}
