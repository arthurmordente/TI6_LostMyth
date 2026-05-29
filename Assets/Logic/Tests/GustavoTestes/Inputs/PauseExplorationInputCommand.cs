using Logic.Scripts.GameDomain.Exploration.Pause;
using Logic.Scripts.Services.CommandFactory;

public class PauseExplorationInputCommand : BaseCommand, ICommandVoid
{
    private IExplorationPauseController _pauseController;
    private IUniversalUIController _universalUIController;

    public override void ResolveDependencies()
    {
        _pauseController = _diContainer.Resolve<IExplorationPauseController>();
        _universalUIController = _diContainer.Resolve<IUniversalUIController>();
    }

    public void Execute()
    {
        if (_pauseController == null) return;

        if (_pauseController.IsVisible)
        {
            _pauseController.HandleEscapeWhilePaused();
            return;
        }

        if (_universalUIController != null && _universalUIController.TryCloseTopOverlay())
            return;

        _pauseController.TogglePauseScreen();
    }
}
