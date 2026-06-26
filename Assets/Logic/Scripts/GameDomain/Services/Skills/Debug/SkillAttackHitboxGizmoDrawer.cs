using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills.Debug
{
    public static class SkillAttackHitboxGizmoDrawer
    {
        const int DiscSegments = 40;

        public static void Draw(SkillAttackHitboxShape shape)
        {
            switch (shape.Kind)
            {
                case SkillAttackHitboxShapeKind.Sphere:
                    DrawWireSphere(shape.Center, shape.Radius, shape.Color);
                    break;
                case SkillAttackHitboxShapeKind.Segment:
                    DrawSegment(shape.Center, shape.SegmentEnd, shape.Color);
                    break;
                case SkillAttackHitboxShapeKind.ColliderBounds:
                    DrawCollider(shape.Collider, shape.Color);
                    break;
            }
        }

        public static void DrawWireSphere(Vector3 center, float radius, Color color)
        {
            if (radius <= 0.0001f) return;
            Gizmos.color = color;
            DrawDiscXZ(center, radius, center.y, DiscSegments);
            DrawVerticalCircle(center, radius, Vector3.right, 24);
            DrawVerticalCircle(center, radius, Vector3.forward, 24);
        }

        public static void DrawSegment(Vector3 origin, Vector3 end, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(origin, end);
            Gizmos.color = new Color(color.r, color.g, color.b, Mathf.Min(1f, color.a * 0.65f));
            Gizmos.DrawSphere(origin, 0.12f);
            Gizmos.DrawSphere(end, 0.12f);
        }

        public static void DrawCollider(Collider collider, Color color)
        {
            if (collider == null || !collider.enabled) return;
            Gizmos.color = color;
            if (collider is SphereCollider sphere)
            {
                Vector3 worldCenter = collider.transform.TransformPoint(sphere.center);
                float worldRadius = sphere.radius * MaxAbsScale(collider.transform.lossyScale);
                DrawWireSphere(worldCenter, worldRadius, color);
                return;
            }

            if (collider is CapsuleCollider capsule)
            {
                DrawCapsuleCollider(capsule, color);
                return;
            }

            if (collider is BoxCollider box)
            {
                Gizmos.matrix = collider.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
                return;
            }

            Bounds bounds = collider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        static void DrawCapsuleCollider(CapsuleCollider capsule, Color color)
        {
            Transform t = capsule.transform;
            Vector3 scale = t.lossyScale;
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float height = Mathf.Max(capsule.height * Mathf.Abs(scale.y), radius * 2f);
            Vector3 center = t.TransformPoint(capsule.center);
            Vector3 axis = capsule.direction switch
            {
                0 => t.right,
                2 => t.forward,
                _ => t.up
            };
            float halfLine = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 a = center - axis * halfLine;
            Vector3 b = center + axis * halfLine;
            Gizmos.color = color;
            Gizmos.DrawLine(a, b);
            DrawWireSphere(a, radius, color);
            DrawWireSphere(b, radius, color);
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

        static void DrawVerticalCircle(Vector3 center, float radius, Vector3 axis, int segments)
        {
            Vector3 tangent = Vector3.Cross(axis, Vector3.up);
            if (tangent.sqrMagnitude < 1e-6f)
                tangent = Vector3.Cross(axis, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(axis, tangent).normalized;
            Vector3 prev = center + tangent * radius;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments * Mathf.PI * 2f;
                Vector3 p = center + tangent * (Mathf.Cos(t) * radius) + bitangent * (Mathf.Sin(t) * radius);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }

        static float MaxAbsScale(Vector3 scale) =>
            Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
    }
}
