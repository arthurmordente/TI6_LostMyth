using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem {
    /// <summary>
    /// Ground-only queries for new skill system aiming (planar combat on the arena floor).
    /// </summary>
    internal static class NewSkillSystemGroundAreaPhysics {
        private const string GroundLayerName = "Ground";

        private static int? _groundLayer;
        private static LayerMask? _groundMask;

        public static LayerMask GroundMask {
            get {
                if (_groundMask.HasValue) return _groundMask.Value;

                _groundLayer ??= LayerMask.NameToLayer(GroundLayerName);
                _groundMask = _groundLayer >= 0 ? (LayerMask)(1 << _groundLayer.Value) : default;
                return _groundMask.Value;
            }
        }

        public static bool HasGroundLayer => GroundMask.value != 0;

        /// <summary>Screen → world point on the Ground layer (ignores triggers).</summary>
        public static bool TryRaycastScreenToGround(Vector2 screenPosition, out Vector3 worldPoint) {
            worldPoint = Vector3.zero;
            if (Camera.main == null || !HasGroundLayer) return false;

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, GroundMask, QueryTriggerInteraction.Ignore))
                return false;

            worldPoint = hit.point;
            return true;
        }

        public static bool TryRaycastMouseToGround(out Vector3 worldPoint) =>
            TryRaycastScreenToGround(Input.mousePosition, out worldPoint);
    }
}
