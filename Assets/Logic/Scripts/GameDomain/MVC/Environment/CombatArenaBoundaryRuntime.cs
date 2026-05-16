using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment
{
    public enum CombatArenaDispositionPolicy
    {
        None = 0,
        /// <summary>Voluntary and forced displacement clamp to playable ring (Laki roulette).</summary>
        LakiHardFullClamp = 1,
        /// <summary>Voluntary clamp only; environment pushes may leave platform; ring-out on fall.</summary>
        HokariVoluntaryPlusRingOut = 2,
    }

    public struct CombatArenaLakiGeometry
    {
        public Vector3 CenterWorld;
        public float InnerRadius;
        public float OuterRadius;
        public float ArcStartDeg;
        public float ArcDeg;
    }

    public struct CombatArenaHokariGeometry
    {
        public Vector3 CenterWorld;
        public float VoluntaryClampRadius;
        public float RingOutFallY;
        public float RingOutOutsideRadiusXZ;
        public int RingOutConsecutiveFrames;
    }

    /// <summary>
    /// Scene-scoped arena bounds registered by Laki or Hokari boss bootstraps.
  /// No-op when inactive.
    /// </summary>
    public static class CombatArenaBoundaryRuntime
    {
        static CombatArenaDispositionPolicy _policy;
        static CombatArenaLakiGeometry _laki;
        static CombatArenaHokariGeometry _hokari;
        static bool _lakiValid;
        static bool _hokariValid;

        public static bool IsActive => _policy != CombatArenaDispositionPolicy.None;

        public static CombatArenaDispositionPolicy Policy => _policy;

        public static bool ClampVoluntaryDisplacement => _policy == CombatArenaDispositionPolicy.LakiHardFullClamp
            || _policy == CombatArenaDispositionPolicy.HokariVoluntaryPlusRingOut;

        public static bool ClampForcedDisplacementEnd => _policy == CombatArenaDispositionPolicy.LakiHardFullClamp;

        public static bool EnableRingOutLoss => _policy == CombatArenaDispositionPolicy.HokariVoluntaryPlusRingOut;

        public static void RegisterLaki(in CombatArenaLakiGeometry geometry)
        {
            _policy = CombatArenaDispositionPolicy.LakiHardFullClamp;
            _laki = geometry;
            _lakiValid = true;
            _hokariValid = false;
        }

        public static void RegisterHokari(in CombatArenaHokariGeometry geometry)
        {
            _policy = CombatArenaDispositionPolicy.HokariVoluntaryPlusRingOut;
            _hokari = geometry;
            _hokariValid = true;
            _lakiValid = false;
        }

        public static void Clear()
        {
            _policy = CombatArenaDispositionPolicy.None;
            _lakiValid = false;
            _hokariValid = false;
        }

        public static bool TryGetHokariGeometry(out CombatArenaHokariGeometry geometry)
        {
            geometry = _hokari;
            return _hokariValid && EnableRingOutLoss;
        }

        /// <summary>
        /// Clamps a world position to where the player may stand (Laki: tile annulus from inner radius outward).
        /// Use for walk, teleport, grapple pull end, and projectile pull — not for skill aim markers.
        /// </summary>
        public static bool TryClampVoluntaryWorldPosition(ref Vector3 worldPosition)
        {
            if (!ClampVoluntaryDisplacement) return false;

            float preservedY = worldPosition.y;

            if (_policy == CombatArenaDispositionPolicy.LakiHardFullClamp && _lakiValid)
            {
                worldPosition = RouletteArenaSpatial.ClampToPlayableRing(
                    worldPosition,
                    _laki.CenterWorld,
                    _laki.InnerRadius,
                    _laki.OuterRadius,
                    _laki.ArcStartDeg,
                    _laki.ArcDeg);
                worldPosition.y = preservedY;
                return true;
            }

            if (_policy == CombatArenaDispositionPolicy.HokariVoluntaryPlusRingOut && _hokariValid)
            {
                worldPosition = ClampToDiscXZ(
                    worldPosition,
                    _hokari.CenterWorld,
                    _hokari.VoluntaryClampRadius);
                worldPosition.y = preservedY;
                return true;
            }

            return false;
        }

        public static bool TryClampForcedWorldPosition(ref Vector3 worldPosition)
        {
            if (!ClampForcedDisplacementEnd) return false;
            return TryClampVoluntaryWorldPosition(ref worldPosition);
        }

        public static bool IsInsideVoluntaryZone(Vector3 worldPosition)
        {
            if (!IsActive) return true;

            if (_policy == CombatArenaDispositionPolicy.LakiHardFullClamp && _lakiValid)
                return RouletteArenaSpatial.ComputeTileIndex(
                    worldPosition,
                    _laki.CenterWorld,
                    _laki.InnerRadius,
                    _laki.OuterRadius,
                    _laki.ArcStartDeg,
                    _laki.ArcDeg) >= 0;

            if (_policy == CombatArenaDispositionPolicy.HokariVoluntaryPlusRingOut && _hokariValid)
            {
                Vector2 rel = new Vector2(
                    worldPosition.x - _hokari.CenterWorld.x,
                    worldPosition.z - _hokari.CenterWorld.z);
                return rel.sqrMagnitude <= _hokari.VoluntaryClampRadius * _hokari.VoluntaryClampRadius;
            }

            return true;
        }

        public static bool ShouldTriggerRingOut(Vector3 worldPosition, int consecutiveOutsideFrames)
        {
            if (!EnableRingOutLoss || !_hokariValid) return false;

            if (worldPosition.y < _hokari.RingOutFallY)
                return true;

            if (_hokari.RingOutOutsideRadiusXZ > 0f)
            {
                Vector2 rel = new Vector2(
                    worldPosition.x - _hokari.CenterWorld.x,
                    worldPosition.z - _hokari.CenterWorld.z);
                if (rel.sqrMagnitude > _hokari.RingOutOutsideRadiusXZ * _hokari.RingOutOutsideRadiusXZ
                    && consecutiveOutsideFrames >= Mathf.Max(1, _hokari.RingOutConsecutiveFrames))
                    return true;
            }

            return false;
        }

        static Vector3 ClampToDiscXZ(Vector3 world, Vector3 center, float radius)
        {
            radius = Mathf.Max(0.01f, radius);
            Vector2 rel = new Vector2(world.x - center.x, world.z - center.z);
            float r = rel.magnitude;
            if (r <= radius || r < 1e-6f)
                return world;
            Vector2 clamped = rel / r * radius;
            return new Vector3(center.x + clamped.x, world.y, center.z + clamped.y);
        }
    }
}
