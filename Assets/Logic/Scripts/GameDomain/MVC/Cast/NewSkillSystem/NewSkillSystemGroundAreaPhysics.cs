using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem {
    /// <summary>
    /// Ground queries for new skill system aiming (planar combat on the arena floor).
    /// </summary>
    internal static class NewSkillSystemGroundAreaPhysics {
        private const string GroundLayerName = "Ground";

        private static int? _groundLayer;
        private static LayerMask? _groundMask;
        private static int? _lakiAimGroundLayer;
        private static LayerMask? _lakiAimGroundMask;
        private static LayerMask? _aimGroundMask;

        public static LayerMask GroundMask {
            get {
                if (_groundMask.HasValue) return _groundMask.Value;

                _groundLayer ??= LayerMask.NameToLayer(GroundLayerName);
                _groundMask = _groundLayer >= 0 ? (LayerMask)(1 << _groundLayer.Value) : default;
                return _groundMask.Value;
            }
        }

        public static LayerMask LakiAimGroundMask {
            get {
                if (_lakiAimGroundMask.HasValue) return _lakiAimGroundMask.Value;

                _lakiAimGroundLayer ??= LayerMask.NameToLayer(LakiArenaAimGroundRuntime.LayerName);
                _lakiAimGroundMask = _lakiAimGroundLayer >= 0
                    ? (LayerMask)(1 << _lakiAimGroundLayer.Value)
                    : default;
                return _lakiAimGroundMask.Value;
            }
        }

        public static LayerMask AimGroundMask {
            get {
                if (_aimGroundMask.HasValue) return _aimGroundMask.Value;
                _aimGroundMask = GroundMask | LakiAimGroundMask;
                return _aimGroundMask.Value;
            }
        }

        public static bool HasAimGroundLayer => AimGroundMask.value != 0;

        /// <summary>Screen → world point on Ground or LakiAimGround (ignores triggers).</summary>
        public static bool TryRaycastScreenToGround(Vector2 screenPosition, out Vector3 worldPoint) {
            worldPoint = Vector3.zero;
            if (Camera.main == null || !HasAimGroundLayer) return false;

            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, AimGroundMask, QueryTriggerInteraction.Ignore))
                return false;

            worldPoint = hit.point;
            return true;
        }

        public static bool TryRaycastMouseToGround(out Vector3 worldPoint) =>
            TryRaycastScreenToGround(Input.mousePosition, out worldPoint);
    }
}
