using UnityEngine;

namespace Logic.Scripts.Core.Mvc.WorldCamera
{
    public interface ICameraFocusService
    {
        bool IsCinematicLockActive { get; }
        bool IsPanActive { get; }

        CameraFocusHandle FocusOn(Transform target, CameraFocusOptions options);
        CameraFocusHandle Follow(Transform target, CameraFocusOptions options);
        void Release(CameraFocusHandle handle);
        void SetDefaultFollow(Transform playerOrActiveUnit);
        void RestoreDefaultFollow();
        void StopFollowing();

        void BeginPan();
        void EndPan();
        void ApplyPanDelta(Vector2 screenDelta);

        void ApplySceneEntry(Transform followTarget, SceneCameraEntrySettings settings);
        Awaitable WaitUntilSceneEntryComplete(System.Threading.CancellationToken cancellationToken);
    }
}
