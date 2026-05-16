using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment
{
    /// <summary>
    /// Temporary scene gizmos for tuning arena bounds on boss bootstraps. Remove when values are final.
    /// </summary>
    public static class CombatArenaBoundaryGizmoDrawer
    {
        const int ArcSegments = 48;

        public static void DrawHokari(
            Vector3 centerWorld,
            float voluntaryRadius,
            float ringOutFallY,
            float ringOutOutsideRadiusXZ)
        {
            float y = centerWorld.y;

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
            DrawDiscXZ(centerWorld, voluntaryRadius, y, ArcSegments);

            if (ringOutOutsideRadiusXZ > 0.01f)
            {
                Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.85f);
                DrawDiscXZ(centerWorld, ringOutOutsideRadiusXZ, y, ArcSegments);
            }

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
            DrawFallPlane(centerWorld, voluntaryRadius, ringOutFallY);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(new Vector3(centerWorld.x, y, centerWorld.z), 0.25f);
        }

        public static void DrawLaki(
            Vector3 centerWorld,
            float innerRadius,
            float outerRadius,
            float arcStartDeg,
            float arcDeg)
        {
            float y = centerWorld.y;
            float arcStartRad = arcStartDeg * Mathf.Deg2Rad;
            float arcRad = Mathf.Clamp(arcDeg, 1f, 360f) * Mathf.Deg2Rad;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            DrawArcRing(centerWorld, y, innerRadius, arcStartRad, arcRad, ArcSegments);

            Gizmos.color = new Color(0.2f, 0.95f, 0.45f, 0.95f);
            DrawArcRing(centerWorld, y, outerRadius, arcStartRad, arcRad, ArcSegments);

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.35f);
            DrawRadialEdges(centerWorld, y, innerRadius, outerRadius, arcStartRad, arcRad);

            float split = RouletteArenaService.ComputeSplitRadius(innerRadius, outerRadius);
            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            DrawArcRing(centerWorld, y, split, arcStartRad, arcRad, Mathf.Max(8, ArcSegments / 2));

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(new Vector3(centerWorld.x, y, centerWorld.z), 0.2f);
        }

        static void DrawDiscXZ(Vector3 center, float radius, float y, int segments)
        {
            if (radius <= 0.01f) return;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            prev.y = y;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments * Mathf.PI * 2f;
                Vector3 p = center + new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius);
                p.y = y;
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }

        static void DrawFallPlane(Vector3 center, float referenceRadius, float fallY)
        {
            float r = Mathf.Max(referenceRadius, 2f);
            Vector3 a = new Vector3(center.x - r, fallY, center.z - r);
            Vector3 b = new Vector3(center.x + r, fallY, center.z - r);
            Vector3 c = new Vector3(center.x + r, fallY, center.z + r);
            Vector3 d = new Vector3(center.x - r, fallY, center.z + r);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        static void DrawArcRing(Vector3 center, float y, float radius, float arcStartRad, float arcRad, int segments)
        {
            if (radius <= 0.01f || arcRad <= 1e-5f) return;
            int steps = Mathf.Max(2, Mathf.RoundToInt(segments * (arcRad / (2f * Mathf.PI))));
            Vector3 prev = PointOnArc(center, y, radius, arcStartRad);
            for (int i = 1; i <= steps; i++)
            {
                float u = i / (float)steps;
                float theta = arcStartRad + arcRad * u;
                Vector3 p = PointOnArc(center, y, radius, theta);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }

        static void DrawRadialEdges(Vector3 center, float y, float inner, float outer, float arcStartRad, float arcRad)
        {
            Vector3 a0 = PointOnArc(center, y, inner, arcStartRad);
            Vector3 b0 = PointOnArc(center, y, outer, arcStartRad);
            Gizmos.DrawLine(a0, b0);

            Vector3 a1 = PointOnArc(center, y, inner, arcStartRad + arcRad);
            Vector3 b1 = PointOnArc(center, y, outer, arcStartRad + arcRad);
            Gizmos.DrawLine(a1, b1);
        }

        static Vector3 PointOnArc(Vector3 center, float y, float radius, float thetaRad)
        {
            return new Vector3(
                center.x + radius * Mathf.Cos(thetaRad),
                y,
                center.z + radius * Mathf.Sin(thetaRad));
        }
    }
}
