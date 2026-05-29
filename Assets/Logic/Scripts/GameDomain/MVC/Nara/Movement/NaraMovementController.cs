using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;

public abstract class NaraMovementController : INaraMovementController, IFixedUpdatable {
    protected readonly IUpdateSubscriptionService UpdateSubscriptionService;
    protected readonly float MoveSpeed;
    protected readonly float RotationSpeed;

    protected Rigidbody NaraRigidbody;
    protected Transform NaraTransform;
    protected GameInputActions GameInputActions;
    protected Vector3 PlanarFacingDirection;

    protected Camera Cam;

    protected bool IsKinematicRigidbody => NaraRigidbody != null && NaraRigidbody.isKinematic;

    public NaraMovementController(GameInputActions inputActions, IUpdateSubscriptionService updateSubscriptionService,
        NaraConfigurationSO naraConfiguration) {
        UpdateSubscriptionService = updateSubscriptionService;
        GameInputActions = inputActions;
        MoveSpeed = naraConfiguration.MoveSpeed;
        RotationSpeed = naraConfiguration.RotationSpeed;
    }

    public virtual void InitEntryPoint(Rigidbody rigidbody, Camera camera) {
        SetNaraRigidbody(rigidbody);
        SetCamera(camera);
    }

    public abstract Vector2 ReadInputs();

    public abstract void Move(Vector2 direction, float velocity, float rotation);
    public abstract void MoveToPoint(Vector3 endPosition, float velocity, float rotation);
    protected void StopPlanarMotion() {
        if (NaraRigidbody == null) return;
        PlanarFacingDirection = Vector3.zero;
        if (!IsKinematicRigidbody)
            NaraRigidbody.linearVelocity = new Vector3(0f, NaraRigidbody.linearVelocity.y, 0f);
    }

    protected void SetPlanarMotion(Vector3 worldDirectionNormalized, float speed) {
        if (NaraRigidbody == null) return;
        PlanarFacingDirection = worldDirectionNormalized;
        if (IsKinematicRigidbody) {
            if (worldDirectionNormalized.sqrMagnitude <= 1e-6f) return;
            Vector3 delta = worldDirectionNormalized * (speed * Time.fixedDeltaTime);
            Vector3 pos = NaraRigidbody.position;
            pos.x += delta.x;
            pos.z += delta.z;
            NaraRigidbody.MovePosition(pos);
            return;
        }

        NaraRigidbody.linearVelocity = new Vector3(
            worldDirectionNormalized.x * speed,
            NaraRigidbody.linearVelocity.y,
            worldDirectionNormalized.z * speed);
    }

    protected void Rotate(float rotationForce) {
        Vector3 rotate = IsKinematicRigidbody
            ? new Vector3(PlanarFacingDirection.x, 0f, PlanarFacingDirection.z)
            : new Vector3(NaraRigidbody.linearVelocity.x, 0f, NaraRigidbody.linearVelocity.z);
        if (rotate.sqrMagnitude > 0.0001f) {
            Quaternion finalRotation = Quaternion.LookRotation(rotate.normalized, Vector3.up);
            NaraTransform.rotation = Quaternion.Slerp(NaraTransform.rotation, finalRotation, Time.fixedDeltaTime * rotationForce);
        }
    }

    private void SetNaraRigidbody(Rigidbody rigidbody) {
        NaraRigidbody = rigidbody;
        NaraTransform = rigidbody != null ? rigidbody.transform : null;
    }

    private void SetCamera(Camera camera) {
        Cam = camera;
    }


    public void ManagedFixedUpdate() {
        Vector2 dir = GameInputActions.Player.Move.ReadValue<Vector2>();
        Move(dir, MoveSpeed, RotationSpeed);
    }

    public void RegisterListeners() {
        UpdateSubscriptionService.RegisterFixedUpdatable(this);
    }

    public void UnregisterListeners() {
        UpdateSubscriptionService.UnregisterFixedUpdatable(this);
    }

    public void DisableInputs() {
        try { GameInputActions.Player.Disable(); } catch { }
    }

    public void EnableInputs() {
        try { GameInputActions.Player.Enable(); } catch { }
    }
}
