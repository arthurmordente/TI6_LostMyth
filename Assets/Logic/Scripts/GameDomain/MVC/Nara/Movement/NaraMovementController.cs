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

    protected Camera Cam;

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
    protected void Rotate(float rotationForce) {
        Vector3 rotate = new Vector3(NaraRigidbody.linearVelocity.x, 0f, NaraRigidbody.linearVelocity.z);
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
