using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.StartLevel;
using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Services.CommandFactory;
using System.Threading;
using UnityEngine;

public class GameOverCommand : BaseCommand, ICommandVoid {
    private IGamePlayUiController _gamePlayUiController;
    private IGameInputActionsController _gameInputActionsController;

    private GameOverCommandData _data;

    public GameOverCommand SetData(GameOverCommandData data) {
        _data = data;
        return this;
    }

    public override void ResolveDependencies() {
        _gamePlayUiController = _diContainer.Resolve<IGamePlayUiController>();
        _gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
    }
    public void Execute() {
        Time.timeScale = 0f;
        _gameInputActionsController.UnregisterGameplayInputListeners();
        _gamePlayUiController.ShowGameOver(_data.IsWin);
    }
}
