using UnityEngine;
namespace Logic.Scripts.Core.Mvc.WorldCamera
{
    public interface IWorldCameraController
    {
        bool IsRotateEnabled { get; }
        void UpdateAngles();
        void StartFollowTarget(Transform targetTransform);
        void StopFollowTarget();
        void UnlockCameraRotate();
        void LockCameraRotate();
        void SetMouseDelta(Vector2 delta);
        void AdjustZoom(float delta);
    }
}
