using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.AttackGizmos
{
    public enum BossAttackDebugShapeKind
    {
        Disc = 0,
        Cone = 1,
        Strip = 2,
    }

    public struct BossAttackDebugShape
    {
        public BossAttackDebugShapeKind Kind;
        public Vector3 Origin;
        public Vector3 Forward;
        public Vector3 StripEnd;
        public float Radius;
        public float AngleDeg;
        public float StripHalfWidth;
        public int ConeSides;
        public Color Color;
        public string Label;
    }
}
