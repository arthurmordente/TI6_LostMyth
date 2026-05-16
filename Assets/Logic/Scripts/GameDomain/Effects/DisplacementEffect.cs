using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Environment;
using System;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Nara;

namespace Assets.Logic.Scripts.GameDomain.Effects {
    [Serializable]
    public class DisplacementEffect : AbilityEffect {
        public float baseDistance;
        public bool isPush; // true = push, false = pull (can be toggled by handler)
        public Vector3 direction; // normalized world direction hint (optional)

        public override void Execute(IEffectable caster, IEffectable target) {
            // Placeholder: we don't have a unified movement API on IEffectable yet.
            // Handlers are expected to compute the final direction and distance and
            // set these fields appropriately prior to executing this effect.

            // Resolve a Transform for target (supports NaraController or MonoBehaviours)
            Transform t = null;
            MonoBehaviour asMono = target as MonoBehaviour;
            if (asMono != null) {
                t = asMono.transform;
            } else {
                NaraController nara = target as NaraController;
                if (nara != null && nara.NaraViewGO != null) {
                    t = nara.NaraViewGO.transform;
                }
            }
            if (t == null) {
                Debug.LogWarning("DisplacementEffect: could not resolve target Transform.");
                return;
            }
            Vector3 planar = new Vector3(direction.x, 0f, direction.z).normalized;
            if (planar.sqrMagnitude < 1e-6f) return;
            float signed = isPush ? 1f : -1f;
            Debug.Log($"DisplacementEffect.Execute -> target={t.name} isPush={isPush} dir={planar} baseDist={baseDistance}");
            // Smooth displacement over a short duration
            MonoBehaviour runnerMb = asMono;
            if (runnerMb == null && t != null)
            {
                runnerMb = t.GetComponent<MonoBehaviour>();
            }
            if (runnerMb != null)
            {
                runnerMb.StartCoroutine(SmoothMove(t, planar * baseDistance * signed, 0.15f));
            }
            else
            {
                // Fallback: immediate move if no MonoBehaviour available to run coroutine
                t.position = t.position + planar * baseDistance * signed;
                Debug.Log($"DisplacementEffect fallback result position -> {t.position}");
            }
        }

        private System.Collections.IEnumerator SmoothMove(Transform t, Vector3 delta, float duration)
        {
            Vector3 startPos = t.position;
            Vector3 endPos = startPos + delta;
            CombatArenaBoundaryRuntime.TryClampForcedWorldPosition(ref endPos);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                t.position = Vector3.Lerp(startPos, endPos, k);
                yield return null;
            }
            t.position = endPos;
            Debug.Log($"DisplacementEffect result position -> {t.position}");
        }
    }
}


