using Logic.Scripts.Services.UpdateService;
using UnityEngine;

namespace Logic.Scripts.Core.Mvc.WorldCamera {
    public class WorldCameraController : IUpdatable, IWorldCameraController {
        private readonly WorldCameraView _worldCameraView;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private bool _rotateEnabled;
        private bool _isRegistered;
        private Vector2 _mouseDelta;
        private Transform _target;
        private GameInputActions _gameInputActions;

        public bool IsRotateEnabled => _rotateEnabled;

        public WorldCameraController(WorldCameraView worldCameraView, GameInputActions gameInputActions, IUpdateSubscriptionService updateSubscriptionService) {
            _worldCameraView = worldCameraView;
            _gameInputActions = gameInputActions;
            _updateSubscriptionService = updateSubscriptionService;
        }

        public void UpdateAngles() {
            // Always update the camera follow proxy so the camera tracks the unit every frame.
            // Only read rotation input when rotation is enabled.
            float rotationDeltaX = 0f;
            if (_rotateEnabled) {
                Vector2 delta = Vector2.zero;
                if (_gameInputActions.Player.enabled == true) delta = _gameInputActions.Player.RotateCam.ReadValue<Vector2>();
                if (_gameInputActions.Exploration.enabled == true) delta = _gameInputActions.Exploration.RotateCam.ReadValue<Vector2>();
                SetMouseDelta(delta);
                rotationDeltaX = _mouseDelta.x;
            }
            _worldCameraView.UpdateCameraRotation(rotationDeltaX, Time.deltaTime);
        }

        public void StartFollowTarget(Transform targetTransform) {
            _target = targetTransform;
            _worldCameraView.SetNewTarget(_target);
            if (!_isRegistered) {
                _updateSubscriptionService.RegisterUpdatable(this);
                _isRegistered = true;
            }
        }

        public void StopFollowTarget() {
            if (_isRegistered) {
                _updateSubscriptionService.UnregisterUpdatable(this);
                _isRegistered = false;
            }
            _target = null;
        }

        public void UnlockCameraRotate() { _rotateEnabled = true; }
        public void LockCameraRotate() { _rotateEnabled = false; }

        public void ManagedUpdate() {
            UpdateAngles();
        }

        public void SetMouseDelta(Vector2 delta) {
            _mouseDelta = delta;
        }

        public void AdjustZoom(float delta) {
            _worldCameraView.AdjustZoom(delta);
        }
    }
}
