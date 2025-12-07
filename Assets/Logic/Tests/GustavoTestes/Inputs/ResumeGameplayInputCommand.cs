using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.Logger.Base;
using UnityEngine;

public class ResumeGameplayInputCommand : BaseCommand, ICommandVoid {
    private IGamePlayUiController _gamePlayUiController;
    private IGameInputActionsController _gameInputActionsController;
    public override void ResolveDependencies() {
        _gamePlayUiController = _diContainer.Resolve<IGamePlayUiController>();
        _gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
    }

    public void Execute() {
        LogService.Log("Resume pressed");
        Time.timeScale = 1f;
        _gamePlayUiController.HidePauseScreen();
        _gameInputActionsController.RegisterGameplayInputListeners();
        _gameInputActionsController.UnregisterUIGameplayInputListeners();
        _gameInputActionsController.DisableUIInputs();
    }
}
