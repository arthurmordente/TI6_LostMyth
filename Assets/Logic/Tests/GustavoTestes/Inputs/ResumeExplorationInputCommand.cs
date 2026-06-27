using Logic.Scripts.GameDomain.Exploration.Pause;
using Logic.Scripts.Services.CommandFactory;

public class ResumeExplorationInputCommand : BaseCommand, ICommandVoid
{
    private IExplorationPauseController _pauseController;
    private IExplorationLoadoutUIController _loadoutUIController;
    private ILobbyTipsUIController _lobbyTipsUIController;

    public override void ResolveDependencies()
    {
        _pauseController = _diContainer.Resolve<IExplorationPauseController>();
        _loadoutUIController = _diContainer.TryResolve<IExplorationLoadoutUIController>();
        _lobbyTipsUIController = _diContainer.TryResolve<ILobbyTipsUIController>();
    }

    public void Execute()
    {
        if (_loadoutUIController != null && _loadoutUIController.IsVisible)
        {
            _loadoutUIController.Hide();
            return;
        }

        if (_lobbyTipsUIController != null && _lobbyTipsUIController.IsVisible)
        {
            _lobbyTipsUIController.Hide();
            return;
        }

        _pauseController?.HandleEscapeWhilePaused();
    }
}
