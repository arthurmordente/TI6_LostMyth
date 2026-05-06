using Logic.Scripts.Services.CommandFactory;

public class OnSkillLoadoutInteractionCommand : BaseCommand, ICommandVoid
{
    private IExplorationLoadoutUIController _explorationLoadoutUIController;

    public override void ResolveDependencies()
    {
        _explorationLoadoutUIController = _diContainer.Resolve<IExplorationLoadoutUIController>();
    }

    public void Execute()
    {
        _explorationLoadoutUIController?.Show();
    }
}
