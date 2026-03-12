using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Logic.Scripts.Services.CommandFactory;

public class MouseClickInputCommand : BaseCommand, ICommandVoid {
    private IActiveUnitService _activeUnitService;
    private ICastController _castController;
    private IDivideAbilityHandler _divideAbilityHandler;

    public override void ResolveDependencies() {
        _activeUnitService = _diContainer.Resolve<IActiveUnitService>();
        _castController = _diContainer.Resolve<ICastController>();
        _divideAbilityHandler = _diContainer.Resolve<IDivideAbilityHandler>();
    }

    public void Execute() {
        // If the Dividir ability is in aiming mode, this click places the book
        if (_divideAbilityHandler != null && _divideAbilityHandler.IsAiming) {
            _divideAbilityHandler.ConfirmPlacement();
            return;
        }

        // Otherwise, confirm the currently aimed ability for the active unit
        var caster = _activeUnitService?.ActiveUnit;
        if (caster == null) return;

        _castController.UseAbility(caster);

        if (_castController?.GetCanUseAbility() == true) {
            caster.OnAbilityExecuted();
            _castController.SetCanUseAbility(false);
        }
    }
}
