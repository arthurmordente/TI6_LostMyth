using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Turns;

/// <summary>
/// Fired when the player presses the Dividir button (bound to the Clone-1 slot key/button).
/// - If the book is not deployed and cooldown is 0: enters aiming mode to place the book.
/// - If the book is deployed and cooldown is 0: recalls the book.
/// - If on cooldown: no-op.
/// </summary>
public class UseDivideAbilityInputCommand : BaseCommand, ICommandVoid
{
    private IDivideAbilityHandler _divideAbilityHandler;
    private ITurnStateReader _turnStateReader;

    public override void ResolveDependencies()
    {
        _divideAbilityHandler = _diContainer.Resolve<IDivideAbilityHandler>();
        _turnStateReader = _diContainer.Resolve<ITurnStateReader>();
    }

    public void Execute()
    {
        if (_turnStateReader == null || _turnStateReader.Phase != TurnPhase.PlayerAct) return;
        _divideAbilityHandler?.Activate();
    }
}
