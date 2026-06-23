using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Logic.Scripts.Services.SceneServices
{
    public static class SceneTransitionReadinessUtility
    {
        public static async Awaitable WaitUntilSceneReady(Scene scene, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Awaitable.NextFrameAsync();
            cancellationToken.ThrowIfCancellationRequested();
            await Awaitable.NextFrameAsync();
            cancellationToken.ThrowIfCancellationRequested();

            if (scene.IsValid())
                TouchSceneMaterials(scene);

            cancellationToken.ThrowIfCancellationRequested();
            await Awaitable.NextFrameAsync();
        }

        static void TouchSceneMaterials(Scene scene)
        {
            var rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                var renderers = rootObjects[i].GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    var materials = renderers[r].sharedMaterials;
                    for (int m = 0; m < materials.Length; m++)
                    {
                        if (materials[m] != null)
                            _ = materials[m].shader;
                    }
                }
            }
        }
    }
}
