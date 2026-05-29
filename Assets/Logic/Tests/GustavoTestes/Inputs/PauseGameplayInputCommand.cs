using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.Logger.Base;
using UnityEngine;

public class PauseGameplayInputCommand : BaseCommand, ICommandVoid {
    private IGamePlayUiController _gamePlayUiController;
    private IGameInputActionsController _gameInputActionsController;
    private IUniversalUIController _universalUIController;

    public override void ResolveDependencies() {
        _gamePlayUiController = _diContainer.Resolve<IGamePlayUiController>();
        _gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
        _universalUIController = _diContainer.Resolve<IUniversalUIController>();
    }

    public void Execute() {
        if (_universalUIController != null && _universalUIController.TryCloseTopOverlay())
            return;

        LogService.Log("Pause pressed");
        Time.timeScale = 0f;
        _gamePlayUiController.ShowPauseScreen();
        _gameInputActionsController.UnregisterGameplayInputListeners();
        _gameInputActionsController.DisableGameplayInputs();
        _gameInputActionsController.EnableUIInputs();
        _gameInputActionsController.RegisterUIGameplayInputListeners();
    }
}
