using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Core.Mvc.WorldCamera;

public class ActivateCamAndCancelAbilityInputCommand : BaseCommand, ICommandVoid {
    private IWorldCameraController _WorldCameraController;
    private ICastController _castController;
    private INaraController _naraController;
    private IDivideAbilityHandler _divideAbilityHandler;

    public override void ResolveDependencies() {
        _WorldCameraController = _diContainer.Resolve<IWorldCameraController>();
        _castController = _diContainer.Resolve<ICastController>();
        _naraController = _diContainer.Resolve<INaraController>();
        _divideAbilityHandler = _diContainer.Resolve<IDivideAbilityHandler>();
    }

    public void Execute() {
        _divideAbilityHandler?.CancelAim();
        _castController.CancelAbilityUse();
        _naraController.Unfreeeze();
        _WorldCameraController.UnlockCameraRotate();
    }
}
