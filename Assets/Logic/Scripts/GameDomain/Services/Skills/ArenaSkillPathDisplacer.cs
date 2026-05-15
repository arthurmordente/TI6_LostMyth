using System;
using System.Collections;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Smoothly moves a Rigidbody to a world position (skill-driven displacement, e.g. projectile arrival).
    /// </summary>
    [DisallowMultipleComponent]
    public class ArenaSkillPathDisplacer : MonoBehaviour
    {
        Coroutine _active;

        public void Begin(Rigidbody rb, Vector3 targetWorld, float durationSeconds, Action onComplete)
        {
            if (rb == null)
            {
                onComplete?.Invoke();
                return;
            }
            if (_active != null)
                StopCoroutine(_active);
            _active = StartCoroutine(Run(rb, targetWorld, Mathf.Max(0.05f, durationSeconds), onComplete));
        }

        IEnumerator Run(Rigidbody rb, Vector3 targetWorld, float duration, Action onComplete)
        {
            Vector3 start = rb.position;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.fixedDeltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                Vector3 p = Vector3.Lerp(start, targetWorld, u);
                rb.MovePosition(p);
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(targetWorld);
            _active = null;
            onComplete?.Invoke();
        }
    }
}
