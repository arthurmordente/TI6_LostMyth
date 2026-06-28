using System.Collections.Generic;
using System.Threading;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Logic.Scripts.Core.Mvc.WorldCamera
{
    public sealed class CameraFocusService : ICameraFocusService, IUpdatable
    {
        sealed class FocusLease
        {
            public int HandleId;
            public Transform Target;
            public CameraFocusOptions Options;
        }

        readonly IWorldCameraController _camera;
        readonly IUpdateSubscriptionService _updates;
        readonly List<FocusLease> _leases = new List<FocusLease>(4);
        Transform _defaultFollow;
        int _nextHandleId = 1;
        bool _panActive;
        bool _updateRegistered;
        bool _sceneEntryPending;

        public bool IsCinematicLockActive =>
            _leases.Count > 0 && _leases[_leases.Count - 1].Options.SuppressPan;

        public bool IsPanActive => _panActive;

        public CameraFocusService(
            IWorldCameraController camera,
            IUpdateSubscriptionService updateSubscriptionService)
        {
            _camera = camera;
            _updates = updateSubscriptionService;
        }

        public CameraFocusHandle FocusOn(Transform target, CameraFocusOptions options) =>
            Acquire(target, options);

        public CameraFocusHandle Follow(Transform target, CameraFocusOptions options) =>
            Acquire(target, options);

        CameraFocusHandle Acquire(Transform target, CameraFocusOptions options)
        {
            if (target == null) return CameraFocusHandle.Invalid;

            var lease = new FocusLease
            {
                HandleId = _nextHandleId++,
                Target = target,
                Options = options
            };
            _leases.Add(lease);
            ApplyTopLease();
            EnsureUpdateRegistered();
            return new CameraFocusHandle(lease.HandleId);
        }

        public void Release(CameraFocusHandle handle)
        {
            if (!handle.IsValid) return;

            for (int i = _leases.Count - 1; i >= 0; i--)
            {
                if (_leases[i].HandleId != handle.Id) continue;
                _leases.RemoveAt(i);
                break;
            }

            ApplyTopLease();
        }

        public void SetDefaultFollow(Transform playerOrActiveUnit)
        {
            _defaultFollow = playerOrActiveUnit;
            if (_leases.Count == 0 && _defaultFollow != null)
            {
                _camera.SetFollowBlendDuration(CameraFocusOptions.DefaultFollow.BlendDuration);
                _camera.StartFollowTarget(_defaultFollow);
                EnsureUpdateRegistered();
            }
        }

        public void RestoreDefaultFollow()
        {
            _leases.Clear();
            _panActive = false;
            _camera.SetExternalInputBlock(false);
            _camera.LockCameraRotate();

            if (_defaultFollow != null)
            {
                _camera.SetFollowBlendDuration(CameraFocusOptions.DefaultFollow.BlendDuration);
                _camera.StartFollowTarget(_defaultFollow);
            }

            _camera.TweenPanOffsetToZero(1f);
            EnsureUpdateRegistered();
        }

        public void StopFollowing()
        {
            _leases.Clear();
            _defaultFollow = null;
            _panActive = false;
            _sceneEntryPending = false;
            _camera.SetExternalInputBlock(false);
            _camera.StopFollowTarget();
            MaybeUnregisterUpdate();
        }

        public void BeginPan()
        {
            if (IsCinematicLockActive) return;
            _panActive = true;
            _camera.SetExternalInputBlock(true);
            _camera.LockCameraRotate();
            EnsureUpdateRegistered();
        }

        public void EndPan()
        {
            if (!_panActive) return;
            _panActive = false;
            _camera.SetExternalInputBlock(false);
            _camera.LockCameraRotate();
            _camera.TweenPanOffsetToZero(1f);
        }

        public void ApplyPanDelta(Vector2 screenDelta)
        {
            if (!_panActive || IsCinematicLockActive) return;
            _camera.ApplyPanDelta(screenDelta);
        }

        public void ApplySceneEntry(Transform followTarget, SceneCameraEntrySettings settings)
        {
            if (followTarget == null) return;

            _leases.Clear();
            _panActive = false;
            _defaultFollow = followTarget;
            _camera.SetExternalInputBlock(false);
            _camera.LockCameraRotate();
            _camera.ApplyOrbitPreset(settings);
            _camera.SetFollowBlendDuration(settings.BlendDuration);
            _camera.StartFollowTarget(followTarget);

            if (settings.BlendDuration <= 0f)
                _camera.CompleteFollowTransitionImmediate();

            _sceneEntryPending = true;
            EnsureUpdateRegistered();
            _camera.UpdateAngles();
        }

        public async Awaitable WaitUntilSceneEntryComplete(CancellationToken cancellationToken)
        {
            if (!_sceneEntryPending)
                return;

            while (!_camera.IsFollowTransitionComplete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _camera.UpdateAngles();
                await Awaitable.NextFrameAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();
            _camera.UpdateAngles();
            await Awaitable.NextFrameAsync();

            _sceneEntryPending = false;
        }

        public void ManagedUpdate()
        {
            if (_panActive && !IsCinematicLockActive)
            {
                Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
                if (delta.sqrMagnitude > 0f)
                    ApplyPanDelta(delta);
            }

            if (!_panActive && _leases.Count == 0 && _defaultFollow == null)
                MaybeUnregisterUpdate();
        }

        void ApplyTopLease()
        {
            if (_leases.Count == 0)
            {
                if (_defaultFollow != null)
                {
                    _camera.SetFollowBlendDuration(CameraFocusOptions.DefaultFollow.BlendDuration);
                    _camera.StartFollowTarget(_defaultFollow);
                }
                _camera.SetExternalInputBlock(false);
                return;
            }

            FocusLease top = _leases[_leases.Count - 1];
            _camera.SetFollowBlendDuration(top.Options.BlendDuration);
            _camera.StartFollowTarget(top.Target);

            bool blockInput = top.Options.SuppressRotation || top.Options.SuppressPan;
            _camera.SetExternalInputBlock(blockInput);
            if (top.Options.SuppressRotation)
                _camera.LockCameraRotate();
        }

        void EnsureUpdateRegistered()
        {
            if (_updateRegistered) return;
            _updates.RegisterUpdatable(this);
            _updateRegistered = true;
        }

        void MaybeUnregisterUpdate()
        {
            if (!_updateRegistered) return;
            if (_panActive || _leases.Count > 0 || _defaultFollow != null) return;
            _updates.UnregisterUpdatable(this);
            _updateRegistered = false;
        }
    }
}
