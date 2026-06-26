using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills.Debug
{
    public enum SkillAttackHitboxShapeKind
    {
        Sphere,
        Segment,
        ColliderBounds
    }

    public readonly struct SkillAttackHitboxShape
    {
        public SkillAttackHitboxShapeKind Kind { get; }
        public Color Color { get; }
        public Vector3 Center { get; }
        public float Radius { get; }
        public Vector3 SegmentEnd { get; }
        public Collider Collider { get; }

        public static SkillAttackHitboxShape Sphere(Vector3 center, float radius, Color color) =>
            new(center, radius, color, SkillAttackHitboxShapeKind.Sphere);

        public static SkillAttackHitboxShape Segment(Vector3 origin, Vector3 end, Color color) =>
            new(origin, end, color, SkillAttackHitboxShapeKind.Segment);

        public static SkillAttackHitboxShape ColliderBounds(Collider collider, Color color) =>
            new(collider, color, SkillAttackHitboxShapeKind.ColliderBounds);

        SkillAttackHitboxShape(Vector3 center, float radius, Color color, SkillAttackHitboxShapeKind kind)
        {
            Kind = kind;
            Color = color;
            Center = center;
            Radius = radius;
            SegmentEnd = default;
            Collider = null;
        }

        SkillAttackHitboxShape(Vector3 origin, Vector3 end, Color color, SkillAttackHitboxShapeKind kind)
        {
            Kind = kind;
            Color = color;
            Center = origin;
            SegmentEnd = end;
            Radius = 0f;
            Collider = null;
        }

        SkillAttackHitboxShape(Collider collider, Color color, SkillAttackHitboxShapeKind kind)
        {
            Kind = kind;
            Color = color;
            Collider = collider;
            Center = default;
            Radius = 0f;
            SegmentEnd = default;
        }
    }
}
