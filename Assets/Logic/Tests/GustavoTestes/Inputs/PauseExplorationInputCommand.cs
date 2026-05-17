using Logic.Scripts.GameDomain.Exploration.QuitConfirmation;
using Logic.Scripts.Services.CommandFactory;

public class PauseExplorationInputCommand : BaseCommand, ICommandVoid
{
    private IExplorationQuitConfirmationService _quitConfirmation;

    public override void ResolveDependencies()
    {
        _quitConfirmation = _diContainer.Resolve<IExplorationQuitConfirmationService>();
    }

    public void Execute()
    {
        _quitConfirmation?.HandleEscapePressed();
    }
}
