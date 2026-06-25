using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.AttackGizmos
{
    public static class BossAttackGizmoDrawer
    {
        const int DefaultDiscSegments = 48;

        public static void Draw(in BossAttackDebugShape shape)
        {
            switch (shape.Kind)
            {
                case BossAttackDebugShapeKind.Disc:
                    DrawDisc(shape.Origin, shape.Radius, shape.Color);
                    break;
                case BossAttackDebugShapeKind.Cone:
                    DrawCone(shape.Origin, shape.Forward, shape.Radius, shape.AngleDeg,
                        shape.ConeSides > 0 ? shape.ConeSides : 24, shape.Color);
                    break;
                case BossAttackDebugShapeKind.Strip:
                    DrawStrip(shape.Origin, shape.StripEnd, shape.StripHalfWidth, shape.Color);
                    break;
            }
        }

        public static void DrawDisc(Vector3 center, float radius, Color color)
        {
            if (radius <= 0.01f) return;
            Gizmos.color = color;
            DrawDiscXZ(center, radius, center.y, DefaultDiscSegments);
        }

        public static void DrawCone(Vector3 origin, Vector3 forward, float radius, float angleDeg, int sides, Color color)
        {
            if (radius <= 0.01f || angleDeg <= 0.01f) return;

            Vector3 planarForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (planarForward.sqrMagnitude < 1e-8f) planarForward = Vector3.forward;
            planarForward.Normalize();

            float y = origin.y;
            Vector3 basePoint = origin + planarForward * radius;
            int clampedSides = Mathf.Max(4, sides);
            float step = angleDeg / clampedSides;

            Gizmos.color = color;
            Vector3 prevArc = Vector3.zero;
            for (int i = 0; i <= clampedSides; i++)
            {
                float currentAngle = -(angleDeg * 0.5f) + (i * step);
                Quaternion rot = Quaternion.Euler(0f, currentAngle, 0f);
                Vector3 arcPoint = origin + (rot * (basePoint - origin));
                arcPoint.y = y;

                if (i == 0)
                {
                    Gizmos.DrawLine(origin, arcPoint);
                    prevArc = arcPoint;
                    continue;
                }

                Gizmos.DrawLine(prevArc, arcPoint);
                if (i == clampedSides)
                    Gizmos.DrawLine(origin, arcPoint);
                prevArc = arcPoint;
            }
        }

        public static void DrawStrip(Vector3 start, Vector3 end, float halfWidth, Color color)
        {
            start.y = end.y;
            Vector3 ab = end - start;
            ab.y = 0f;
            if (ab.sqrMagnitude < 1e-8f) return;

            Vector3 tangent = ab.normalized;
            Vector3 lateral = Vector3.Cross(tangent, Vector3.up).normalized;
            float hw = Mathf.Max(0.05f, halfWidth);

            Vector3 a = start + lateral * hw;
            Vector3 b = start - lateral * hw;
            Vector3 c = end - lateral * hw;
            Vector3 d = end + lateral * hw;

            Gizmos.color = color;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
            Gizmos.DrawLine(start, end);
        }

        static void DrawDiscXZ(Vector3 center, float radius, float y, int segments)
        {
            Vector3 prev = new Vector3(center.x + radius, y, center.z);
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments * Mathf.PI * 2f;
                Vector3 p = new Vector3(center.x + Mathf.Cos(t) * radius, y, center.z + Mathf.Sin(t) * radius);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
