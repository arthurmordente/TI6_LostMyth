using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.Logger.Base;

public class PauseExplorationInputCommand : BaseCommand, ICommandVoid
{
    public override void ResolveDependencies()
    {
        
    }

    public void Execute()
    {
        LogService.Log("Pause pressed");
    }
}
