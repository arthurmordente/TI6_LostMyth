using Logic.Scripts.Services.UpdateService;
using UnityEngine;

namespace Logic.Scripts.Core.Mvc.WorldCamera
{
    public class WorldCameraController : IUpdatable, IWorldCameraController
    {
        private readonly WorldCameraView _worldCameraView;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private bool _rotateEnabled;
        private Vector2 _mouseDelta;
        private Transform _target;
        private GameInputActions _gameInputActions;

        public bool IsRotateEnabled => _rotateEnabled;

        public WorldCameraController(WorldCameraView worldCameraView, GameInputActions gameInputActions, IUpdateSubscriptionService updateSubscriptionService)
        {
            _worldCameraView = worldCameraView;
            _gameInputActions = gameInputActions;
            _updateSubscriptionService = updateSubscriptionService;
        }

        public void UpdateAngles()
        {
            if (!_rotateEnabled) return;
            Vector2 delta = _gameInputActions.Player.RotateCam.ReadValue<Vector2>();
            SetMouseDelta(delta);
            _worldCameraView.UpdateCameraRotation(_mouseDelta.x, Time.deltaTime);
        }

        public void StartFollowTarget(Transform targetTransform)
        {
            _target = targetTransform;
            _worldCameraView.SetNewTarget(_target);
            _updateSubscriptionService.RegisterUpdatable(this);
        }

        public void StopFollowTarget()
        {
            _updateSubscriptionService.UnregisterUpdatable(this);
            _target = null;
        }

        public void UnlockCameraRotate() { _rotateEnabled = true; }
        public void LockCameraRotate()   { _rotateEnabled = false; }

        public void ManagedUpdate()
        {
            UpdateAngles();
        }

        public void SetMouseDelta(Vector2 delta)
        {
            _mouseDelta = delta;
        }

        public void AdjustZoom(float delta)
        {
            _worldCameraView.AdjustZoom(delta);
        }
    }
}
