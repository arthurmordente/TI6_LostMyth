using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Cast.Paschoal {
    internal static class PaschoalSkillAimWorld {
        public static bool TryMouseHitPoint(out Vector3 worldPoint) {
            worldPoint = Vector3.zero;
            if (Camera.main == null) return false;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return false;
            worldPoint = hit.point;
            return true;
        }

        public static Vector3 GetFallbackAimPoint(IPlayableUnit playable) {
            var t = playable?.UnitViewGO != null ? playable.UnitViewGO.transform : null;
            if (t == null) return Vector3.zero;
            return t.position + t.forward * 2f;
        }

        public static Vector3 ResolveAimPoint(IPlayableUnit playable, out bool fromMouse) {
            if (TryMouseHitPoint(out Vector3 hit)) {
                fromMouse = true;
                return hit;
            }
            fromMouse = false;
            return GetFallbackAimPoint(playable);
        }

        public static Vector3 GetSkillOrigin(IPlayableUnit playable, IEffectable caster) {
            if (playable?.UnitSkillSpotTransform != null)
                return playable.UnitSkillSpotTransform.position;
            if (caster != null)
                return caster.GetTransformCastPoint() != null
                    ? caster.GetTransformCastPoint().position
                    : caster.GetReferenceTransform().position;
            return Vector3.zero;
        }

        public static float GetMaxDirectedDistance(SkillDataSO skill) {
            if (skill == null) return 0f;
            return skill.Range > 0.0001f ? skill.Range : 500f;
        }

        public static Vector3 ClampDirectedEnd(Vector3 origin, Vector3 aimPoint, float maxDistance) {
            Vector3 delta = aimPoint - origin;
            float mag = delta.magnitude;
            if (mag <= maxDistance || mag < 1e-5f) return aimPoint;
            return origin + delta.normalized * maxDistance;
        }
    }
}
