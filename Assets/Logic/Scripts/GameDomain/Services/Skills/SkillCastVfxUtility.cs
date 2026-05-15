using System.Collections;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Spawned Area impact / Self cast VFX: by default destroyed after one-shot playback.
    /// </summary>
    public static class SkillCastVfxUtility
    {
        public static void ConfigureSpawnedInstance(GameObject instance, bool persistInScene, float destroyAfterSeconds)
        {
            if (instance == null || persistInScene) return;
            if (destroyAfterSeconds > 0.0001f)
            {
                Object.Destroy(instance, destroyAfterSeconds);
                return;
            }
            var host = instance.GetComponent<SkillCastTransientVfx>();
            if (host == null)
                host = instance.AddComponent<SkillCastTransientVfx>();
            host.ActivateAutoDestroy();
        }
    }

    [DisallowMultipleComponent]
    public class SkillCastTransientVfx : MonoBehaviour
    {
        public void ActivateAutoDestroy()
        {
            StopAllCoroutines();
            StartCoroutine(DestroyWhenFinished());
        }

        IEnumerator DestroyWhenFinished()
        {
            const float fallbackSeconds = 3f;
            var particles = GetComponentsInChildren<ParticleSystem>(true);
            if (particles == null || particles.Length == 0)
            {
                yield return new WaitForSeconds(fallbackSeconds);
                Destroy(gameObject);
                yield break;
            }

            bool anyAlive = true;
            while (anyAlive)
            {
                anyAlive = false;
                for (int i = 0; i < particles.Length; i++)
                {
                    var ps = particles[i];
                    if (ps != null && ps.IsAlive(true))
                    {
                        anyAlive = true;
                        break;
                    }
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
