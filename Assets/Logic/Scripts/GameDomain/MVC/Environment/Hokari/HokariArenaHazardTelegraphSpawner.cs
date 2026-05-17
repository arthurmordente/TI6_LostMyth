using Logic.Scripts.GameDomain.MVC.Boss.Telegraph;
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari
{
    public static class HokariArenaHazardTelegraphSpawner
    {
        const float GroundYOffset = 0.05f;

        public static GameObject Spawn(
            HokariArenaHazardDefinitionSO definition,
            HokariArenaHazardPatternSO pattern,
            CombatAttackVisualCatalogSO catalog,
            Vector3 arenaCenter,
            float arenaRadiusXZ,
            INaraController nara,
            out Vector3 telegraphAnchorWorld)
        {
            telegraphAnchorWorld = arenaCenter;
            if (definition == null || catalog == null) return null;

            GameObject prefab = definition.CatalogTelegraph.ResolvePrefab(catalog);
            if (prefab == null) return null;

            if (!TryResolveSpawnPosition(definition.TelegraphSpawn, arenaCenter, arenaRadiusXZ, nara, out Vector3 worldPos))
                return null;

            worldPos.y = ResolveGroundY(nara, worldPos.y);
            telegraphAnchorWorld = worldPos;

            var instance = Object.Instantiate(prefab, worldPos, Quaternion.identity);
            instance.name = $"HokariHazardTelegraph_{definition.name}";

            float scale = pattern != null
                ? pattern.ResolveTelegraphDiscRadius(definition)
                : Mathf.Max(0.1f, definition.TelegraphDiscRadius);
            instance.transform.localScale = new Vector3(scale, 1f, scale);

            var layering = TelegraphLayeringLocator.Service;
            if (layering != null)
            {
                var layer = layering.Register(preferTop: true);
                var mrs = instance.GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0; i < mrs.Length; i++)
                {
                    if (mrs[i] == null) continue;
                    var mat = mrs[i].material;
                    if (mat != null) mat.renderQueue += layer.QueueAdd;
                }
            }

            // Must persist until HokariArenaHazardRuntimeSchedule destroys it on hazard resolve (not VFX auto-destroy).
            StripTransientAutoDestroy(instance);
            SkillCastVfxUtility.ConfigureSpawnedInstance(instance, persistInScene: true, destroyAfterSeconds: 0f);
            return instance;
        }

        static void StripTransientAutoDestroy(GameObject instance)
        {
            if (instance == null) return;
            var transients = instance.GetComponentsInChildren<SkillCastTransientVfx>(true);
            for (int i = 0; i < transients.Length; i++)
            {
                if (transients[i] != null)
                    Object.Destroy(transients[i]);
            }
        }

        public static bool TryResolvePullAnchor(
            HokariArenaHazardDefinitionSO definition,
            Vector3 arenaCenter,
            float arenaRadiusXZ,
            INaraController nara,
            out Vector3 pullAnchorWorld)
        {
            pullAnchorWorld = arenaCenter;
            if (definition == null) return false;
            if (!TryResolveSpawnPosition(definition.TelegraphSpawn, arenaCenter, arenaRadiusXZ, nara, out Vector3 worldPos))
                return false;
            worldPos.y = ResolveGroundY(nara, worldPos.y);
            pullAnchorWorld = worldPos;
            return true;
        }

        static float ResolveGroundY(INaraController nara, float fallbackY)
        {
            if (nara?.NaraViewGO != null)
                return nara.NaraViewGO.transform.position.y + GroundYOffset;
            return fallbackY + GroundYOffset;
        }

        public static bool TryResolveSpawnPosition(
            HokariArenaHazardTelegraphSpawnMode mode,
            Vector3 arenaCenter,
            float arenaRadiusXZ,
            INaraController nara,
            out Vector3 worldPos)
        {
            worldPos = arenaCenter;
            arenaRadiusXZ = Mathf.Max(0.5f, arenaRadiusXZ);

            Vector3 player = arenaCenter;
            if (nara?.NaraViewGO != null)
                player = nara.NaraViewGO.transform.position;

            Vector2 relPlayer = new Vector2(player.x - arenaCenter.x, player.z - arenaCenter.z);

            switch (mode)
            {
                case HokariArenaHazardTelegraphSpawnMode.AtPlayerFeet:
                    worldPos = new Vector3(player.x, player.y, player.z);
                    return true;

                case HokariArenaHazardTelegraphSpawnMode.OppositePlayerAcrossArena:
                {
                    if (relPlayer.sqrMagnitude < 1e-6f)
                        relPlayer = Vector2.right * 0.5f;
                    Vector2 opposite = -relPlayer.normalized * (arenaRadiusXZ * 0.85f);
                    worldPos = new Vector3(arenaCenter.x + opposite.x, player.y, arenaCenter.z + opposite.y);
                    return true;
                }

                case HokariArenaHazardTelegraphSpawnMode.RandomInArena:
                {
                    float r = arenaRadiusXZ * Mathf.Sqrt(Random.value) * 0.9f;
                    float angle = Random.value * Mathf.PI * 2f;
                    worldPos = new Vector3(
                        arenaCenter.x + Mathf.Cos(angle) * r,
                        player.y,
                        arenaCenter.z + Mathf.Sin(angle) * r);
                    return true;
                }

                case HokariArenaHazardTelegraphSpawnMode.AtArenaCenter:
                    worldPos = new Vector3(arenaCenter.x, player.y, arenaCenter.z);
                    return true;

                default:
                    return false;
            }
        }
    }
}
