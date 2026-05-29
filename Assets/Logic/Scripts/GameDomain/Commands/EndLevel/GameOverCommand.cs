using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.StartLevel;
using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using System.Threading;
using UnityEngine;

public class GameOverCommand : BaseCommand, ICommandVoid {
    private IGamePlayUiController _gamePlayUiController;
    private IGameInputActionsController _gameInputActionsController;
    private IAudioService _audioService;

    private GameOverCommandData _data;

    public GameOverCommand SetData(GameOverCommandData data) {
        _data = data;
        return this;
    }

    public override void ResolveDependencies() {
        _gamePlayUiController = _diContainer.Resolve<IGamePlayUiController>();
        _gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
        try { _audioService = _diContainer.Resolve<IAudioService>(); } catch { _audioService = null; }
    }
    public void Execute() {
        Time.timeScale = 0f;
        _gameInputActionsController.UnregisterGameplayInputListeners();
        GeneralSfxFeedback.PlayGameOverStinger(_audioService, _data.IsWin);
        _gamePlayUiController.ShowGameOver(_data.IsWin);
    }
}
