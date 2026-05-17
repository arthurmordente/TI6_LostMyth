using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment
{
    /// <summary>
    /// Snaps world XZ to the combat floor (Ground layer) so turn-mode gravity freeze does not leave units floating.
    /// </summary>
    public static class CombatGroundPositionSnap
    {
        const string GroundLayerName = "Ground";
        const float RaycastOriginLift = 3f;
        const float RaycastMaxDistance = 16f;

        static int? _groundLayer;
        static LayerMask? _groundMask;

        static LayerMask GroundMask
        {
            get
            {
                if (_groundMask.HasValue) return _groundMask.Value;
                _groundLayer ??= LayerMask.NameToLayer(GroundLayerName);
                _groundMask = _groundLayer >= 0 ? (LayerMask)(1 << _groundLayer.Value) : default;
                return _groundMask.Value;
            }
        }

        public static bool HasGroundLayer => GroundMask.value != 0;

        /// <summary>Raycasts down at <paramref name="worldPosition"/> XZ and returns a point on the floor.</summary>
        public static Vector3 SnapWorldPosition(Vector3 worldPosition, float footYOffset = 0f)
        {
            if (TrySnapWorldPosition(worldPosition, footYOffset, out Vector3 snapped))
                return snapped;
            return worldPosition;
        }

        public static bool TrySnapWorldPosition(Vector3 worldPosition, float footYOffset, out Vector3 snapped)
        {
            snapped = worldPosition;
            Vector3 origin = worldPosition + Vector3.up * RaycastOriginLift;

            if (HasGroundLayer
                && Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, RaycastMaxDistance, GroundMask,
                    QueryTriggerInteraction.Ignore))
            {
                snapped = groundHit.point;
                snapped.y += footYOffset;
                return true;
            }

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit fallbackHit, RaycastMaxDistance, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                snapped = fallbackHit.point;
                snapped.y += footYOffset;
                return true;
            }

            return false;
        }
    }
}
