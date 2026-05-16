using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>
    /// Shared polar arena math (same rules as <see cref="RouletteArenaService.ComputeTileIndex"/>).
    /// </summary>
    public static class RouletteArenaSpatial
    {
        const int SectorCount = 8;
        const int RadialBands = 2;

        public static int ComputeTileIndex(
            Vector3 worldPos,
            Vector3 centerWorld,
            float innerRadius,
            float outerRadius,
            float arcStartDeg,
            float arcDeg)
        {
            Vector2 rel = new Vector2(worldPos.x - centerWorld.x, worldPos.z - centerWorld.z);
            float r = rel.magnitude;
            if (r < innerRadius || r > outerRadius) return -1;

            float theta = Mathf.Atan2(rel.y, rel.x);
            if (theta < 0f) theta += 2f * Mathf.PI;

            float arcStartRad = arcStartDeg * Mathf.Deg2Rad;
            float arcRad = Mathf.Clamp(arcDeg, 1f, 360f) * Mathf.Deg2Rad;
            float sectorAngleRad = arcRad / SectorCount;

            float relTheta = theta - arcStartRad;
            if (relTheta < 0f) relTheta += 2f * Mathf.PI;
            if (relTheta >= arcRad) return -1;

            int sectorIndex = Mathf.Clamp(Mathf.FloorToInt(relTheta / sectorAngleRad), 0, SectorCount - 1);
            float split = RouletteArenaService.ComputeSplitRadius(innerRadius, outerRadius);
            int band = r < split ? 0 : 1;
            return sectorIndex * RadialBands + band;
        }

        public static Vector3 ClampToPlayableRing(
            Vector3 worldPos,
            Vector3 centerWorld,
            float innerRadius,
            float outerRadius,
            float arcStartDeg,
            float arcDeg)
        {
            innerRadius = Mathf.Max(0.01f, innerRadius);
            outerRadius = Mathf.Max(innerRadius + 0.01f, outerRadius);

            Vector2 rel = new Vector2(worldPos.x - centerWorld.x, worldPos.z - centerWorld.z);
            float r = rel.magnitude;
            float theta = r > 1e-6f ? Mathf.Atan2(rel.y, rel.x) : 0f;
            if (theta < 0f) theta += 2f * Mathf.PI;

            float arcStartRad = arcStartDeg * Mathf.Deg2Rad;
            float arcRad = Mathf.Clamp(arcDeg, 1f, 360f) * Mathf.Deg2Rad;

            float relTheta = theta - arcStartRad;
            if (relTheta < 0f) relTheta += 2f * Mathf.PI;

            if (relTheta >= arcRad)
            {
                float d0 = relTheta;
                float d1 = 2f * Mathf.PI - relTheta;
                relTheta = d0 <= d1 ? 0f : arcRad - 1e-4f;
            }

            theta = arcStartRad + relTheta;
            r = Mathf.Clamp(r, innerRadius, outerRadius);

            float x = centerWorld.x + r * Mathf.Cos(theta);
            float z = centerWorld.z + r * Mathf.Sin(theta);
            return new Vector3(x, worldPos.y, z);
        }
    }
}
