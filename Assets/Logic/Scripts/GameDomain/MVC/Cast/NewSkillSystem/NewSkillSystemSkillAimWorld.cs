using Logic.Scripts.GameDomain.MVC.Environment;
using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem {
    internal static class NewSkillSystemSkillAimWorld {
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

        /// <summary>Self-cast aim / VFX anchor at the playable unit root (feet).</summary>
        public static Vector3 GetSelfCastFootWorld(IPlayableUnit playable) {
            if (playable?.UnitViewGO == null) return Vector3.zero;
            return playable.UnitViewGO.transform.position;
        }

        public static float GetMaxDirectedDistance(SkillDataSO skill) {
            if (skill == null) return 0f;
            return skill.GetProjectileRange();
        }

        public static Vector3 ClampDirectedEnd(Vector3 origin, Vector3 aimPoint, float maxDistance) {
            Vector3 delta = aimPoint - origin;
            float mag = delta.magnitude;
            if (mag <= maxDistance || mag < 1e-5f) return aimPoint;
            return origin + delta.normalized * maxDistance;
        }

        /// <summary>
        /// Directed skills use a horizontal aim vector so spawn rotation and line preview stay stable
        /// (3D look-at from a high cast point to the ground skews <see cref="Quaternion.LookRotation"/>).
        /// </summary>
        public static Vector3 GetPlanarDirectionFromOriginToAim(IPlayableUnit playable, IEffectable caster) {
            Vector3 origin = GetSkillOrigin(playable, caster);
            Vector3 aim = ResolveAimPoint(playable, out _);
            Vector3 d = aim - origin;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-8f && playable?.UnitViewGO != null) {
                d = Vector3.ProjectOnPlane(playable.UnitViewGO.transform.forward, Vector3.up);
            }
            if (d.sqrMagnitude < 1e-8f)
                d = Vector3.forward;
            return d.normalized;
        }

        /// <summary>End of the directed segment: along planar dir, clamped by skill range and true horizontal distance to aim.</summary>
        public static Vector3 GetPlanarClampedAimEnd(IPlayableUnit playable, IEffectable caster, SkillDataSO skill) {
            Vector3 origin = GetSkillOrigin(playable, caster);
            Vector3 aim = ResolveAimPoint(playable, out _);
            Vector3 delta = aim - origin;
            delta.y = 0f;
            float maxDist = GetMaxDirectedDistance(skill);
            float mag = delta.magnitude;
            if (mag < 1e-8f)
                return origin + GetPlanarDirectionFromOriginToAim(playable, caster) * Mathf.Min(maxDist, 2f);
            Vector3 dir = delta / mag;
            float travel = Mathf.Min(maxDist, mag);
            Vector3 end = origin + dir * travel;
            CombatArenaBoundaryRuntime.TryClampVoluntaryWorldPosition(ref end);
            return end;
        }

        public static Vector3 GetAreaClampedAimPoint(IPlayableUnit playable, IEffectable caster, SkillDataSO skill)
        {
            Vector3 origin = GetSkillOrigin(playable, caster);
            Vector3 aim = ResolveAimPoint(playable, out _);
            Vector3 delta = aim - origin;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist < 1e-8f)
                return origin;

            float minDist = skill.GetAreaMinCastDistance();
            float maxDist = skill.GetAreaMaxCastDistance();
            float clamped = Mathf.Clamp(dist, minDist, maxDist);
            Vector3 result = origin + delta.normalized * clamped;
            CombatArenaBoundaryRuntime.TryClampVoluntaryWorldPosition(ref result);
            return result;
        }
    }
}
