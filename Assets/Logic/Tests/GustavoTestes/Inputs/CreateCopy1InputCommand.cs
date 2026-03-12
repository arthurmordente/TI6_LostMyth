using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Turns;

/// <summary>
/// Previously: create fast echo.
/// Now: activate the Dividir ability (deploy or recall the Book).
/// Bound to the Clone-1 slot key (configurable in the Unity Input Actions asset).
/// </summary>
public class CreateCopy1InputCommand : BaseCommand, ICommandVoid {
    private IDivideAbilityHandler _divideAbilityHandler;
    private ITurnStateReader _turnStateReader;
    private ICastController _castController;

    public override void ResolveDependencies() {
        _divideAbilityHandler = _diContainer.Resolve<IDivideAbilityHandler>();
        _turnStateReader = _diContainer.Resolve<ITurnStateReader>();
        _castController = _diContainer.Resolve<ICastController>();
    }

    public void Execute() {
        if (_turnStateReader == null || _turnStateReader.Phase != TurnPhase.PlayerAct) return;

        // Cancel any regular ability cast that might be in progress before entering aiming mode.
        _castController?.CancelAbilityUse();

        _divideAbilityHandler?.Activate();
    }
}
