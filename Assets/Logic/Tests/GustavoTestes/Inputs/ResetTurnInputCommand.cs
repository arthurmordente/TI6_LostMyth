using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.GameDomain.MVC.Nara;

public class ResetTurnInputCommand : BaseCommand, ICommandVoid {
    private INaraController _naraController;

    public override void ResolveDependencies() {
        _naraController = _diContainer.Resolve<INaraController>();
    }

    public void Execute() {
        if (_naraController?.NaraMove is NaraTurnMovementController naraTurnMovement) _naraController.SetPosition(naraTurnMovement.GetNaraCenter());
    }
}
