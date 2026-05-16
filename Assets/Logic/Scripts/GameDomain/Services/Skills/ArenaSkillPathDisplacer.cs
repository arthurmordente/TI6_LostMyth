using System;
using System.Collections;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Smoothly moves a Rigidbody to a world position (skill-driven displacement, e.g. projectile pull).
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
            _active = StartCoroutine(RunWrapped(rb, targetWorld, durationSeconds, onComplete));
        }

        IEnumerator RunWrapped(Rigidbody rb, Vector3 targetWorld, float durationSeconds, Action onComplete)
        {
            yield return ArenaBoundedPlanarDisplacement.Run(rb, targetWorld, durationSeconds, onComplete);
            _active = null;
        }
    }
}
