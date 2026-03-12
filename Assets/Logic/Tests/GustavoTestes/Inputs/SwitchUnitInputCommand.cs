using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Turns;

/// <summary>
/// Fired when the player presses TAB.
/// Toggles active control between Nara and the Book (only while the book is deployed
/// and it is the player's turn).
/// </summary>
public class SwitchUnitInputCommand : BaseCommand, ICommandVoid
{
    private IActiveUnitService _activeUnitService;
    private ITurnStateReader _turnStateReader;

    public override void ResolveDependencies()
    {
        _activeUnitService = _diContainer.Resolve<IActiveUnitService>();
        _turnStateReader = _diContainer.Resolve<ITurnStateReader>();
    }

    public void Execute()
    {
        if (_activeUnitService == null || !_activeUnitService.IsBookDeployed) return;
        if (_turnStateReader == null || _turnStateReader.Phase != TurnPhase.PlayerAct) return;

        _activeUnitService.ToggleActiveUnit();
    }
}
