using Logic.Scripts.Services.UpdateService;
using UnityEngine;

namespace Logic.Scripts.Core.Mvc.WorldCamera
{
    public class WorldCameraController : IUpdatable, IWorldCameraController
    {
        readonly WorldCameraView _worldCameraView;
        readonly IUpdateSubscriptionService _updateSubscriptionService;
        readonly GameInputActions _gameInputActions;
        bool _rotateEnabled;
        bool _externalInputBlock;
        bool _isRegistered;
        Vector2 _mouseDelta;
        Transform _target;

        public bool IsRotateEnabled => _rotateEnabled && !_externalInputBlock;

        public WorldCameraController(
            WorldCameraView worldCameraView,
            GameInputActions gameInputActions,
            IUpdateSubscriptionService updateSubscriptionService)
        {
            _worldCameraView = worldCameraView;
            _gameInputActions = gameInputActions;
            _updateSubscriptionService = updateSubscriptionService;
        }

        public void UpdateAngles()
        {
            float rotationDeltaX = 0f;
            if (IsRotateEnabled)
            {
                Vector2 delta = Vector2.zero;
                if (_gameInputActions.Player.enabled)
                    delta = _gameInputActions.Player.RotateCam.ReadValue<Vector2>();
                if (_gameInputActions.Exploration.enabled)
                    delta = _gameInputActions.Exploration.RotateCam.ReadValue<Vector2>();
                SetMouseDelta(delta);
                rotationDeltaX = _mouseDelta.x;
            }

            _worldCameraView.UpdateCameraRotation(rotationDeltaX, Time.deltaTime);
        }

        public void StartFollowTarget(Transform targetTransform)
        {
            _target = targetTransform;
            _worldCameraView.SetNewTarget(_target);
            if (!_isRegistered)
            {
                _updateSubscriptionService.RegisterUpdatable(this);
                _isRegistered = true;
            }
        }

        public void StopFollowTarget()
        {
            if (_isRegistered)
            {
                _updateSubscriptionService.UnregisterUpdatable(this);
                _isRegistered = false;
            }

            _target = null;
            _worldCameraView.SetTargetNull();
        }

        public void UnlockCameraRotate()
        {
            if (_externalInputBlock) return;
            _rotateEnabled = true;
        }

        public void LockCameraRotate() => _rotateEnabled = false;

        public void ManagedUpdate() => UpdateAngles();

        public void SetMouseDelta(Vector2 delta) => _mouseDelta = delta;

        public void AdjustZoom(float delta) => _worldCameraView.AdjustZoom(delta);

        public void SetFollowBlendDuration(float durationSeconds) =>
            _worldCameraView.SetFollowBlendDuration(durationSeconds);

        public void ApplyPanDelta(Vector2 screenDelta) =>
            _worldCameraView.ApplyPanDelta(screenDelta);

        public void TweenPanOffsetToZero(float durationSeconds) =>
            _worldCameraView.TweenPanOffsetToZero(durationSeconds);

        public void SetExternalInputBlock(bool blocked) => _externalInputBlock = blocked;
    }
}
