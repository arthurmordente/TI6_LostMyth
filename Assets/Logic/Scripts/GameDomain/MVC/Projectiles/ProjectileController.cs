using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;
using Zenject;

public abstract class ProjectileController : MonoBehaviour, IFixedUpdatable {
    [field: SerializeField] public float InitialSpeed { get; protected set; }
    [field: SerializeField] public Rigidbody GetRigidbody { get; protected set; }
    protected IEffectable Caster;
    protected AbilityData Data;
    [Inject]
    private IUpdateSubscriptionService _subscriptionService;

    public virtual void Initialize(Transform castTransform, IEffectable caster, AbilityData data) {
        Caster = caster;
        Data = data;
    }

    private void UnregisterOnUpdate() {
        if (_subscriptionService == null) return;
        _subscriptionService.UnregisterFixedUpdatable(this);
    }

    public abstract void ManagedFixedUpdate();

    private void OnTriggerEnter(Collider other) {
        // Ignore the caster's own colliders so the projectile doesn't self-destruct
        // immediately after spawning inside the CastPoint trigger volume.
        if (Caster != null) {
            var casterRoot = Caster.GetReferenceTransform();
            if (casterRoot != null &&
                (other.transform == casterRoot || other.transform.IsChildOf(casterRoot)))
                return;
        }
        OnHit(other);
    }

    public virtual void OnHit(Collider other) {
        UnregisterOnUpdate();
        Destroy(gameObject);
    }

}
