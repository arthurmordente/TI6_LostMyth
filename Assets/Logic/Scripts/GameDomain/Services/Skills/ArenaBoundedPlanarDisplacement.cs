using System;
using System.Collections;
using Logic.Scripts.GameDomain.MVC.Environment;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Planar rigidbody move with voluntary arena bounds: stop and apply a short knockback when blocked.
    /// Used by projectile caster pull (<see cref="ArenaSkillPathDisplacer"/>).
    /// </summary>
    public static class ArenaBoundedPlanarDisplacement
    {
        const float BoundaryBlockEpsilonSq = 0.0004f;
        const float BoundaryKnockbackMeters = 0.42f;
        const float BoundaryKnockbackDurationSeconds = 0.14f;
        const float ArrivalKnockbackMeters = 0.35f;
        const float ArrivalKnockbackDurationSeconds = 0.12f;

        public static void ZeroPlanarVelocity(Rigidbody rb)
        {
            if (rb == null || rb.isKinematic) return;
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }

        public static IEnumerator Run(Rigidbody rb, Vector3 targetWorld, float durationSeconds, Action onComplete)
        {
            if (rb == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            GuidedDisplacementGate.Enter();
            try
            {
                Vector3 start = rb.position;
                float preservedY = start.y;
                targetWorld.y = preservedY;

                float duration = Mathf.Max(0.05f, durationSeconds);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.fixedDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    Vector3 desired = Vector3.Lerp(start, targetWorld, t);
                    desired.y = preservedY;

                    Vector3 clamped = desired;
                    bool blocked = CombatArenaBoundaryRuntime.TryClampVoluntaryWorldPosition(ref clamped)
                        && (clamped - desired).sqrMagnitude > BoundaryBlockEpsilonSq;

                    if (blocked)
                    {
                        clamped.y = preservedY;
                        rb.MovePosition(clamped);
                        yield return KnockbackAlong(rb, preservedY, clamped, ResolveBlockKnockbackDirection(clamped, desired));
                        ZeroPlanarVelocity(rb);
                        onComplete?.Invoke();
                        yield break;
                    }

                    rb.MovePosition(desired);
                    yield return new WaitForFixedUpdate();
                }

                Vector3 finalPos = targetWorld;
                CombatArenaBoundaryRuntime.TryClampVoluntaryWorldPosition(ref finalPos);
                finalPos.y = preservedY;
                rb.MovePosition(finalPos);

                Vector3 arrivalPush = ResolveArrivalKnockbackDirection(start, finalPos);
                if (arrivalPush.sqrMagnitude > 1e-8f)
                    yield return KnockbackAlong(rb, preservedY, finalPos, arrivalPush, ArrivalKnockbackMeters, ArrivalKnockbackDurationSeconds);

                ZeroPlanarVelocity(rb);
                onComplete?.Invoke();
            }
            finally
            {
                GuidedDisplacementGate.Exit();
            }
        }

        static Vector3 ResolveBlockKnockbackDirection(Vector3 clampedPos, Vector3 attemptedPos)
        {
            Vector3 dir = clampedPos - attemptedPos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-8f)
                return dir.normalized;

            return Vector3.zero;
        }

        /// <summary>Small push away from the pull destination (arrived near the hit target).</summary>
        static Vector3 ResolveArrivalKnockbackDirection(Vector3 pullStart, Vector3 destination)
        {
            Vector3 dir = pullStart - destination;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-8f)
                return Vector3.zero;
            return dir.normalized;
        }

        static IEnumerator KnockbackAlong(
            Rigidbody rb,
            float preservedY,
            Vector3 fromPos,
            Vector3 direction,
            float distanceMeters = BoundaryKnockbackMeters,
            float durationSeconds = BoundaryKnockbackDurationSeconds)
        {
            if (direction.sqrMagnitude < 1e-8f)
                yield break;

            Vector3 dirN = direction.normalized;
            Vector3 end = fromPos + dirN * distanceMeters;
            end.y = preservedY;
            CombatArenaBoundaryRuntime.TryClampVoluntaryWorldPosition(ref end);
            end.y = preservedY;

            float elapsed = 0f;
            float duration = Mathf.Max(0.02f, durationSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.fixedDeltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                Vector3 p = Vector3.Lerp(fromPos, end, k);
                p.y = preservedY;
                rb.MovePosition(p);
                yield return new WaitForFixedUpdate();
            }

            rb.MovePosition(end);
            ZeroPlanarVelocity(rb);
        }
    }
}
