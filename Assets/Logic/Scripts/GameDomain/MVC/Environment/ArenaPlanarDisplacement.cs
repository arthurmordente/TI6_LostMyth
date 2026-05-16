using System;
using DG.Tweening;
using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment
{
    public enum ArenaPlanarDirectionMode
    {
        RadialOutFromPoint = 0,
        RadialInToPoint = 1,
        WorldDirectionXZ = 2,
        AwayFromTransform = 3,
        TowardTransform = 4,
        AlongStripNormal = 5,
    }

    [Serializable]
    public struct PlanarPushRequest
    {
        public float DistanceMeters;
        public float DurationSeconds;
        public ArenaPlanarDirectionMode DirectionMode;
        public Vector3 ReferenceWorldPoint;
        public Vector3 WorldDirectionXZ;
        public Transform ReferenceTransform;
        public Vector3 LineStart;
        public Vector3 LineEnd;
        public bool MultiplyByDebuffStacks;
    }

    /// <summary>
    /// Environment / arena pushes with explicit metres and duration (no boss attack statics).
    /// </summary>
    public static class ArenaPlanarDisplacement
    {
        const float DefaultDuration = 0.45f;

        public static bool TryApply(Rigidbody rb, in PlanarPushRequest request, IEffectable targetForStacks = null)
        {
            if (rb == null) return false;
            if (!TryResolveDirection(rb.position, in request, out Vector3 dir))
                return false;

            float distance = Mathf.Max(0f, request.DistanceMeters);
            if (request.MultiplyByDebuffStacks && targetForStacks is NaraController nc)
            {
                int stacks = Mathf.Clamp(nc.GetDebuffStacks(), 0, 5);
                distance *= 1f + stacks * 0.2f;
            }

            if (distance <= 1e-6f) return false;

            Vector3 start = rb.position;
            Vector3 end = start + dir * distance;
            end.y = start.y;

            float duration = request.DurationSeconds > 0f ? request.DurationSeconds : DefaultDuration;
            DOTween.Kill(rb, complete: false);
            DOVirtual.Float(0f, 1f, duration, v =>
            {
                Vector3 p = Vector3.Lerp(start, end, v);
                p.y = start.y;
                rb.MovePosition(p);
            })
            .SetEase(Ease.Linear)
            .SetUpdate(UpdateType.Fixed)
            .SetId(rb);
            return true;
        }

        public static bool TryResolveDirection(Vector3 bodyWorld, in PlanarPushRequest request, out Vector3 dir)
        {
            dir = Vector3.zero;
            switch (request.DirectionMode)
            {
                case ArenaPlanarDirectionMode.RadialOutFromPoint:
                    dir = bodyWorld - request.ReferenceWorldPoint;
                    dir.y = 0f;
                    break;
                case ArenaPlanarDirectionMode.RadialInToPoint:
                    dir = request.ReferenceWorldPoint - bodyWorld;
                    dir.y = 0f;
                    break;
                case ArenaPlanarDirectionMode.WorldDirectionXZ:
                    dir = request.WorldDirectionXZ;
                    dir.y = 0f;
                    break;
                case ArenaPlanarDirectionMode.AwayFromTransform:
                    if (request.ReferenceTransform == null) return false;
                    dir = bodyWorld - request.ReferenceTransform.position;
                    dir.y = 0f;
                    break;
                case ArenaPlanarDirectionMode.TowardTransform:
                    if (request.ReferenceTransform == null) return false;
                    dir = request.ReferenceTransform.position - bodyWorld;
                    dir.y = 0f;
                    break;
                case ArenaPlanarDirectionMode.AlongStripNormal:
                {
                    Vector3 a = request.LineStart;
                    Vector3 b = request.LineEnd;
                    Vector3 ab = b - a;
                    ab.y = 0f;
                    float t = Mathf.Clamp01(Vector3.Dot(bodyWorld - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
                    Vector3 closest = a + ab * t;
                    Vector3 axis = ab.sqrMagnitude > 1e-6f ? ab.normalized : Vector3.forward;
                    Vector3 normal = new Vector3(-axis.z, 0f, axis.x);
                    float side = Mathf.Sign(Vector3.Dot(normal, bodyWorld - closest));
                    if (Mathf.Abs(side) < 1e-6f) side = 1f;
                    dir = side * normal;
                    break;
                }
                default:
                    return false;
            }

            if (dir.sqrMagnitude < 1e-8f) return false;
            dir.Normalize();
            return true;
        }
    }
}
