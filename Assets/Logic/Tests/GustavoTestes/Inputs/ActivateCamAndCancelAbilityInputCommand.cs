using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Logic.Scripts.Services.CommandFactory;

public class ActivateCamAndCancelAbilityInputCommand : BaseCommand, ICommandVoid {
    private IWorldCameraController _WorldCameraController;
    private ICastController _castController;
    private INaraController _naraController;
    private IActiveUnitService _activeUnitService;
    private IDivideAbilityHandler _divideAbilityHandler;

    public override void ResolveDependencies() {
        _WorldCameraController = _diContainer.Resolve<IWorldCameraController>();
        _castController = _diContainer.Resolve<ICastController>();
        _naraController = _diContainer.Resolve<INaraController>();
        _activeUnitService = _diContainer.Resolve<IActiveUnitService>();
        _divideAbilityHandler = _diContainer.Resolve<IDivideAbilityHandler>();
    }

    public void Execute() {
        _divideAbilityHandler?.CancelAim();
        _castController.CancelAbilityUse();

        // Unfreeze whichever unit is currently active (Nara or Book).
        // Previously only Nara was unfrozen, leaving the Book stuck after a cancelled cast.
        var active = _activeUnitService?.ActiveUnit;
        if (active != null)
            active.Unfreeeze();
        else
            _naraController.Unfreeeze(); // fallback

        _WorldCameraController.UnlockCameraRotate();
    }
}
