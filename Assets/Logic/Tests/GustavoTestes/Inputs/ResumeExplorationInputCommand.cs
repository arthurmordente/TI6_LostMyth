using Logic.Scripts.GameDomain.Exploration.Pause;
using Logic.Scripts.Services.CommandFactory;

public class ResumeExplorationInputCommand : BaseCommand, ICommandVoid
{
    private IExplorationPauseController _pauseController;

    public override void ResolveDependencies()
    {
        _pauseController = _diContainer.Resolve<IExplorationPauseController>();
    }

    public void Execute()
    {
        _pauseController?.HandleEscapeWhilePaused();
    }
}
