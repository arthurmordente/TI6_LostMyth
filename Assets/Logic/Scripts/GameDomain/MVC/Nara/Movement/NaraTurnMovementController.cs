using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;
using Logic.Scripts.Turns;
using Logic.Scripts.Services.UpdateService;
using Zenject;

public class NaraTurnMovementController : NaraMovementController {
    private Vector3 _movement;
    private Vector3 _movementCenter;
    private int _movementRadius;
    private int _initialMovementRadius;
    private ICheatController _cheatController;

    public NaraAreaLineHandlerController LineHandlerController;
    private ActionPointsService _actionPointsService;
    private int _extraMovementSpaceCost = 2;
    private TurnStateService _turnStateService;

    public NaraTurnMovementController(GameInputActions inputActions, IUpdateSubscriptionService updateSubscriptionService,
        NaraConfigurationSO naraConfiguration, ICheatController cheatController) : base(inputActions, updateSubscriptionService, naraConfiguration) {
        _initialMovementRadius = naraConfiguration.InitialMovementDistance;
        LineHandlerController = new NaraAreaLineHandlerController(naraConfiguration, updateSubscriptionService);
        _cheatController = cheatController;
    }

    public override void InitEntryPoint(Rigidbody rigidbody, Camera camera) {
        base.InitEntryPoint(rigidbody, camera);
        _movementRadius = _initialMovementRadius;
        LineHandlerController.InitEntryPoint(NaraTransform);
        // Ensure movement center starts at current position to avoid unintended clamps before first player phase
        SetMovementRadiusCenter();
    }
    public override Vector2 ReadInputs() {
        return GameInputActions.Player.Move.ReadValue<Vector2>();
    }

    public void SetActionPointsService(ActionPointsService actionPointsService) {
        _actionPointsService = actionPointsService;
    }

    public Vector3 GetNaraCenter() {
        return _movementCenter;
    }

    public int GetNaraRadius() {
        return _movementRadius;
    }

    public void SetNaraRadius(int value) {
        _movementRadius = value;
    }

    public void ResetMovementArea() {
        ResetMovementRadius();
        SetMovementRadiusCenter();
        LineHandlerController.Refresh(_movementCenter, _movementRadius, NaraTransform.position);
    }
    public void Refresh() {
        LineHandlerController.Refresh(_movementCenter, _movementRadius, NaraTransform.position);
    }

    public void CheckRadiusLimit() {
        float distance = Vector3.Distance(NaraTransform.position, _movementCenter);

        if (distance > _movementRadius) {
            Debug.Log("Passou Raio");
            Vector3 directionFromCenter = (NaraTransform.position - _movementCenter).normalized;
            Vector3 radiusLimit = _movementCenter + directionFromCenter * _movementRadius;
            NaraRigidbody.MovePosition(new Vector3(radiusLimit.x, NaraTransform.position.y, radiusLimit.z));
        }
    }

    public override void Move(Vector2 direction, float velocity, float rotation) {
        if (NaraRigidbody == null || NaraTransform == null) return;
        if (!IsPlayerActPhase()) {
            // hard lock: zero planar velocity when not in player phase
            NaraRigidbody.linearVelocity = new Vector3(0f, NaraRigidbody.linearVelocity.y, 0f);
            return;
        }

        Vector3 camF = Cam.transform.forward;
        camF.y = 0f; camF.Normalize();
        Vector3 camR = Cam.transform.right;
        camR.y = 0f; camR.Normalize();

        Vector3 worldDir = camF * direction.y + camR * direction.x;
        if (worldDir.sqrMagnitude > 1e-6f) worldDir.Normalize();

        float distance = Vector3.Distance(NaraTransform.position, _movementCenter);

        Vector3 vel = worldDir * velocity;
        NaraRigidbody.linearVelocity = new Vector3(vel.x, NaraRigidbody.linearVelocity.y, vel.z);

        Rotate(rotation);
        CheckRadiusLimit();
    }


    public override void MoveToPoint(Vector3 endPosition, float velocity, float rotation) {
        if (!IsPlayerActPhase()) {
            return;
        }
        float distance = Vector3.Distance(endPosition, _movementCenter);

        if (distance < _movementRadius) {
            if (distance >= _movementRadius / 2) {
                _actionPointsService.Spend(_extraMovementSpaceCost);
            }
            Vector3 direction = endPosition - NaraTransform.position;
            direction.y = 0f;
            Vector3 n = direction.magnitude > 0.0001f ? direction.normalized : Vector3.zero;
            _movement = n * velocity;
            NaraRigidbody.linearVelocity = new Vector3(_movement.x, 0f, _movement.z);
            Rotate(rotation);
        }
    }

    public void SetMovementRadiusCenter() {
        if (NaraTransform != null) {
            _movementCenter = NaraTransform.position;
        }
    }

    public void RecalculateRadiusAfterAbility() {
        if (NaraTransform != null) {
            float distance = Vector3.Distance(NaraTransform.position, _movementCenter);
            _movementCenter = NaraTransform.position;
            _movementRadius -= (int)distance;
        }
    }

    public void ResetMovementRadius() {
        if (_cheatController.InfinityMove) RemoveMovementRadius();
        else _movementRadius = _initialMovementRadius;
    }

    public void RemoveMovementRadius() {
        _movementRadius = 10000;
    }

    public void SetRadiusToZero() {
        _movementRadius = 0;
    }

    public void ActivateNaraGravity() {
        NaraRigidbody.useGravity = true;
        NaraRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionY;
    }

    public void DeactivateNaraGravity() {
        NaraRigidbody.useGravity = false;
        NaraRigidbody.constraints |= RigidbodyConstraints.FreezePositionY;
    }

    public bool IsMovementAllowed() {
        return IsPlayerActPhase();
    }

    private bool IsPlayerActPhase() {
        if (_turnStateService == null) {
            // Prefer SceneContext container (scene-scoped bindings), fallback to ProjectContext
            try {
                SceneContext sceneContainer = null;
                var contexts = UnityEngine.Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None);
                for (int i = 0; i < contexts.Length; i++) {
                    var sc = contexts[i];
                    if (sc != null && sc.gameObject.scene == NaraTransform.gameObject.scene) { sceneContainer = sc; break; }
                }
                if (sceneContainer != null) {
                    _turnStateService = sceneContainer.Container.Resolve<TurnStateService>();
                }
                else {
                    _turnStateService = ProjectContext.Instance.Container.Resolve<TurnStateService>();
                }
            }
            catch { _turnStateService = null; }
        }
        return _turnStateService != null && _turnStateService.Phase == TurnPhase.PlayerAct;
    }
}
