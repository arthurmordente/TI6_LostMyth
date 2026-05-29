using Logic.Scripts.GameDomain.Exploration.Pause;
using Logic.Scripts.Services.CommandFactory;

public class ResumeExplorationInputCommand : BaseCommand, ICommandVoid
{
    private IExplorationPauseController _pauseController;
    private IExplorationLoadoutUIController _loadoutUIController;

    public override void ResolveDependencies()
    {
        _pauseController = _diContainer.Resolve<IExplorationPauseController>();
        _loadoutUIController = _diContainer.TryResolve<IExplorationLoadoutUIController>();
    }

    public void Execute()
    {
        if (_loadoutUIController != null && _loadoutUIController.IsVisible)
        {
            _loadoutUIController.Hide();
            return;
        }

        _pauseController?.HandleEscapeWhilePaused();
    }
}
