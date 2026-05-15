using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Logic.Scripts.Services.CommandFactory;
using UnityEngine.EventSystems;

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

        // Mesmo clique do botão de skill no canvas não deve confirmar o cast — só prepara; confirma num clique no jogo.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Otherwise, confirm the currently aimed ability for the active unit
        var caster = _activeUnitService?.ActiveUnit;
        if (caster == null) return;

        _castController.UseAbility(caster);

        if (_castController?.GetCanUseAbility() == true) {
            if (_castController.ConsumeDeferredArenaSyncAfterProjectileCast())
                caster.Unfreeeze();
            else if (_castController.ConsumeLastCastWasMovementSkill())
                caster.SyncArenaMovementAfterMovementSkillDisplacement();
            else
                caster.OnAbilityExecuted();
            _castController.SetCanUseAbility(false);
        }
    }
}
