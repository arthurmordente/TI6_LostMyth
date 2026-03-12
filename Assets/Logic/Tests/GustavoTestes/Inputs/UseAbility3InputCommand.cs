using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Logic.Scripts.Services.CommandFactory;

public class UseAbility3InputCommand : BaseCommand, ICommandVoid {
    private const int ABILITY_INDEX = 2;

    private IActiveUnitService _activeUnitService;
    private ICastController _castController;
    private IDivideAbilityHandler _divideAbilityHandler;

    public override void ResolveDependencies() {
        _activeUnitService = _diContainer.Resolve<IActiveUnitService>();
        _castController = _diContainer.Resolve<ICastController>();
        _divideAbilityHandler = _diContainer.Resolve<IDivideAbilityHandler>();
    }

    public void Execute() {
        var caster = _activeUnitService?.ActiveUnit;
        if (caster == null) return;

        _divideAbilityHandler?.CancelAim();

        _castController.CancelAbilityUse();
        if (_castController.TryUseAbility(ABILITY_INDEX, caster)) {
            caster.Freeeze();
        }
    }
}
