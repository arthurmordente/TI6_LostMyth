using UnityEngine;

namespace Logic.Scripts.Utils
{
    public static class DeathAnimationAwaiter
    {
        public const string DefaultDeathTag = "Death";
        public const float DefaultNormalizedThreshold = 0.95f;
        public const float DefaultTimeoutSeconds = 10f;

        public static async Awaitable WaitUntilComplete(
            Animator animator,
            string deathTag = DefaultDeathTag,
            float normalizedThreshold = DefaultNormalizedThreshold,
            float timeoutSeconds = DefaultTimeoutSeconds,
            int layer = 0)
        {
            if (animator == null) return;

            deathTag ??= DefaultDeathTag;
            normalizedThreshold = Mathf.Clamp01(normalizedThreshold);
            timeoutSeconds = Mathf.Max(0.01f, timeoutSeconds);

            float elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                if (!animator.IsInTransition(layer))
                {
                    var state = animator.GetCurrentAnimatorStateInfo(layer);
                    if (state.IsTag(deathTag))
                        break;
                }

                elapsed += Time.deltaTime;
                await Awaitable.NextFrameAsync();
            }

            elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                if (!animator.IsInTransition(layer))
                {
                    var state = animator.GetCurrentAnimatorStateInfo(layer);
                    if (state.IsTag(deathTag) && state.normalizedTime >= normalizedThreshold)
                        return;
                }

                elapsed += Time.deltaTime;
                await Awaitable.NextFrameAsync();
            }
        }
    }
}
