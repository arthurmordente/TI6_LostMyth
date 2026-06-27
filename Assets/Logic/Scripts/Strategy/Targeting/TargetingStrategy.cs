using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;

public abstract class TargetingStrategy : IUpdatable {
    protected AbilityData Ability;
    protected IEffectable Caster;
    protected IUpdateSubscriptionService SubscriptionService;
    protected LayerMask CastableLayerMask;
    protected ICommandFactory CommandFactory;

    public virtual void Initialize(AbilityData data, IEffectable caster) {
        Ability = data;
        Caster = caster;
        Caster.GetTransformCastPoint().rotation = Quaternion.identity;
    }

    public virtual void SetUp(IUpdateSubscriptionService updateSubscriptionService, ICommandFactory commandFactory) {
        SubscriptionService = updateSubscriptionService;
        CommandFactory = commandFactory;
    }

    public virtual Vector3 LockAim(out IEffectable[] targets) {
        targets = null;
        return Vector3.zero;
    }

    public virtual bool TryGetAimPreviewPosition(out Vector3 position) {
        position = default;
        return false;
    }

    public virtual void Cancel() {
        Ability = null;
        SubscriptionService.UnregisterUpdatable(this);
    }

    public virtual void ManagedUpdate() {

    }
}
