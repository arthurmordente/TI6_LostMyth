using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.StateMachineService;

public class ToggleExplorationLoadoutUICommand : BaseCommand, ICommandVoid
{
    private IExplorationLoadoutUIController _loadoutUIController;
    private IStateMachineService _stateMachineService;

    public override void ResolveDependencies()
    {
        _stateMachineService = _diContainer.Resolve<IStateMachineService>();
        _loadoutUIController = _diContainer.Resolve<IExplorationLoadoutUIController>();
    }

    public void Execute()
    {
        if (_stateMachineService?.CurrentState()?.GameStateType != GameStateType.Exploration) return;
        _loadoutUIController?.Toggle();
    }
}
