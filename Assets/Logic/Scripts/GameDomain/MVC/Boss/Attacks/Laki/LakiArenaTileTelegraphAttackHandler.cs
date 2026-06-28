using System.Collections;
using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Boss;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core;
using Logic.Scripts.GameDomain.MVC.Boss.Telegraph;
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.MVC.Boss.Attacks.Laki
{
    /// <summary>Round telegraphs on Laki roulette tile centers; hit when the player is within the disc radius from each area center.</summary>
    public sealed class LakiArenaTileTelegraphAttackHandler : IBossAttackHandler, ITelegraphVisibility
    {
        const float GroundYOffset = 0.05f;

        struct StrikeSlot
        {
            public int TileIndex;
            public Vector3 Center;
            public GameObject Telegraph;
            public float DiscRadius;
            /// <summary>World XZ hit radius in meters (visual size at spawn time).</summary>
            public float HitRadius;
        }

        readonly int _areaCount;
        readonly float _playerTileChance;
        readonly float _telegraphDiscRadiusMin;
        readonly float _telegraphDiscRadiusMax;
        readonly float _hitRadiusMetersAtUnitDisc;
        readonly float _hitRadiusPadding;
        readonly float _telegraphSpawnInterval;
        readonly float _strikeResolveInterval;
        readonly CombatAttackVisualCatalogSO _catalog;
        readonly GameObject _telegraphPrefabFallback;
        readonly MonoBehaviour _coroutineHost;
        readonly int _tileSelectionSeed;

        readonly List<StrikeSlot> _strikes = new List<StrikeSlot>(8);
        Coroutine _spawnRoutine;
        bool _visible;

        public LakiArenaTileTelegraphAttackHandler(
            int areaCount,
            float playerTileChance,
            float telegraphDiscRadiusMin,
            float telegraphDiscRadiusMax,
            float hitRadiusMetersAtUnitDisc,
            float hitRadiusPadding,
            float telegraphSpawnInterval,
            float strikeResolveInterval,
            CombatAttackVisualCatalogSO catalog,
            GameObject telegraphPrefabFallback,
            MonoBehaviour coroutineHost,
            int tileSelectionSeed)
        {
            _areaCount = Mathf.Max(1, areaCount);
            _playerTileChance = Mathf.Clamp01(playerTileChance);
            _telegraphDiscRadiusMin = Mathf.Max(0.1f, telegraphDiscRadiusMin);
            _telegraphDiscRadiusMax = Mathf.Max(_telegraphDiscRadiusMin, telegraphDiscRadiusMax);
            _hitRadiusMetersAtUnitDisc = hitRadiusMetersAtUnitDisc > 0.01f ? hitRadiusMetersAtUnitDisc : 3f;
            _hitRadiusPadding = Mathf.Max(0f, hitRadiusPadding);
            _telegraphSpawnInterval = Mathf.Max(0f, telegraphSpawnInterval);
            _strikeResolveInterval = Mathf.Max(0f, strikeResolveInterval);
            _catalog = catalog;
            _telegraphPrefabFallback = telegraphPrefabFallback;
            _coroutineHost = coroutineHost;
            _tileSelectionSeed = tileSelectionSeed;
        }

        public void PrepareTelegraph(Transform parentTransform)
        {
            StopSpawnRoutine();
            ClearTelegraphObjects();
            _strikes.Clear();
            TelegraphVisibilityRegistry.Register(this);
        }

        public bool ComputeHits(ArenaPosReference arenaReference, Transform originTransform, IEffectable caster) =>
            IsPlayerInsideAnyStrike(arenaReference);

        public IEnumerator ExecuteEffects(
            List<AbilityEffect> effects,
            ArenaPosReference arenaReference,
            Transform originTransform,
            IEffectable caster)
        {
            if (effects == null || effects.Count == 0)
            {
                Debug.LogWarning("[LakiTileTelegraph] No effects configured on BossAttack prefab.");
                yield break;
            }
            if (_strikes.Count == 0)
            {
                Debug.LogWarning("[LakiTileTelegraph] No strikes planned — telegraphs may not have finished spawning.");
                yield break;
            }

            IEffectable target = ResolvePlayerEffectable(arenaReference);
            if (target == null)
            {
                Debug.LogWarning("[LakiTileTelegraph] Player IEffectable not resolved (check ArenaPosReference / INaraController).");
                yield break;
            }

            for (int s = 0; s < _strikes.Count; s++)
            {
                StrikeSlot strike = _strikes[s];
                float hitRadius = GetLiveStrikeHitRadius(strike);
                if (DoesStrikeHitPlayer(arenaReference, strike, hitRadius, out _))
                {
                    for (int i = 0; i < effects.Count; i++)
                    {
                        var fx = effects[i];
                        if (fx == null) continue;
                        if (fx is IAsyncEffect asyncFx) yield return asyncFx.ExecuteRoutine(caster, target);
                        else fx.Execute(caster, target);
                    }
                }

                DismissStrikeTelegraph(s);

                if (_strikeResolveInterval > 0f && s < _strikes.Count - 1)
                    yield return new WaitForSeconds(_strikeResolveInterval);
            }
        }

        public void Cleanup()
        {
            StopSpawnRoutine();
            ClearTelegraphObjects();
            _strikes.Clear();
            TelegraphVisibilityRegistry.Unregister(this);
        }

        public void SetTelegraphVisible(bool visible)
        {
            _visible = visible;
            if (!visible)
            {
                StopSpawnRoutine();
                for (int i = 0; i < _strikes.Count; i++)
                {
                    var slot = _strikes[i];
                    if (slot.Telegraph != null) slot.Telegraph.SetActive(false);
                    _strikes[i] = slot;
                }
                return;
            }

            if (_strikes.Count > 0)
            {
                for (int i = 0; i < _strikes.Count; i++)
                {
                    var slot = _strikes[i];
                    if (slot.Telegraph != null) slot.Telegraph.SetActive(true);
                    _strikes[i] = slot;
                }
                return;
            }

            StopSpawnRoutine();
            ClearTelegraphObjects();
            _strikes.Clear();

            if (_coroutineHost != null && _coroutineHost.isActiveAndEnabled)
                _spawnRoutine = _coroutineHost.StartCoroutine(SpawnTelegraphsSequentialRoutine());
            else
                SpawnAllTelegraphsImmediate();
        }

        IEnumerator SpawnTelegraphsSequentialRoutine()
        {
            var arenaView = Object.FindFirstObjectByType<LakiRouletteArenaView>();
            GameObject telegraphPrefab = ResolveTelegraphPrefab();
            if (arenaView == null || telegraphPrefab == null) yield break;

            int tileCount = Mathf.Max(1, arenaView.TileCount);
            int areasToPlace = Mathf.Min(_areaCount, tileCount);
            yield return SpawnStrikeTilesRoutine(arenaView, telegraphPrefab, areasToPlace, _telegraphSpawnInterval);
            _spawnRoutine = null;
        }

        void SpawnAllTelegraphsImmediate()
        {
            var arenaView = Object.FindFirstObjectByType<LakiRouletteArenaView>();
            GameObject telegraphPrefab = ResolveTelegraphPrefab();
            if (arenaView == null || telegraphPrefab == null) return;

            int tileCount = Mathf.Max(1, arenaView.TileCount);
            int areasToPlace = Mathf.Min(_areaCount, tileCount);
            SpawnStrikeTilesImmediate(arenaView, telegraphPrefab, areasToPlace);
        }

        IEnumerator SpawnStrikeTilesRoutine(
            LakiRouletteArenaView arenaView,
            GameObject telegraphPrefab,
            int areasToPlace,
            float delayBetweenSpawns)
        {
            int tileCount = Mathf.Max(1, arenaView.TileCount);
            var usedTiles = new HashSet<int>();
            var rng = new System.Random(_tileSelectionSeed);

            for (int i = 0; i < areasToPlace; i++)
            {
                if (!TryPickAndAddStrike(arenaView, telegraphPrefab, tileCount, usedTiles, rng))
                    break;
                if (delayBetweenSpawns > 0f && i < areasToPlace - 1)
                    yield return new WaitForSeconds(delayBetweenSpawns);
            }
        }

        void SpawnStrikeTilesImmediate(LakiRouletteArenaView arenaView, GameObject telegraphPrefab, int areasToPlace)
        {
            int tileCount = Mathf.Max(1, arenaView.TileCount);
            var usedTiles = new HashSet<int>();
            var rng = new System.Random(_tileSelectionSeed);

            for (int i = 0; i < areasToPlace; i++)
            {
                if (!TryPickAndAddStrike(arenaView, telegraphPrefab, tileCount, usedTiles, rng))
                    break;
            }
        }

        bool TryPickAndAddStrike(
            LakiRouletteArenaView arenaView,
            GameObject telegraphPrefab,
            int tileCount,
            HashSet<int> usedTiles,
            System.Random rng)
        {
            int playerTile = arenaView.ComputeTileIndex(ResolvePlayerWorldPosition());
            bool rollPlayer = playerTile >= 0 && rng.NextDouble() < _playerTileChance;
            int tile = rollPlayer && !usedTiles.Contains(playerTile)
                ? playerTile
                : PickRandomTile(tileCount, usedTiles, rng);
            if (tile < 0) return false;
            usedTiles.Add(tile);

            Vector3 center = arenaView.GetTileWorldCenter(tile);
            center.y = ResolveGroundY(center.y);
            float discRadius = RollDiscRadius(rng);
            float hitRadius = ResolveHitRadiusMeters(discRadius);
            _strikes.Add(new StrikeSlot
            {
                TileIndex = tile,
                Center = center,
                Telegraph = SpawnTelegraphInstance(telegraphPrefab, tile, center, discRadius),
                DiscRadius = discRadius,
                HitRadius = hitRadius,
            });
            return true;
        }

        float RollDiscRadius(System.Random rng)
        {
            if (_telegraphDiscRadiusMax <= _telegraphDiscRadiusMin + 0.001f)
                return _telegraphDiscRadiusMin;
            float t = (float)rng.NextDouble();
            return Mathf.Lerp(_telegraphDiscRadiusMin, _telegraphDiscRadiusMax, t);
        }

        float ResolveHitRadiusMeters(float discRadius) =>
            discRadius * _hitRadiusMetersAtUnitDisc + _hitRadiusPadding;

        float GetLiveStrikeHitRadius(StrikeSlot strike) => strike.HitRadius;

        GameObject SpawnTelegraphInstance(GameObject prefab, int tileIndex, Vector3 center, float discRadius)
        {
            var instance = Object.Instantiate(prefab, center, Quaternion.identity);
            instance.name = $"LakiTileTelegraph_{tileIndex}";
            instance.transform.localScale = new Vector3(discRadius, 1f, discRadius);
            ApplyTelegraphLayering(instance);
            StripTransientAutoDestroy(instance);
            SkillCastVfxUtility.ConfigureSpawnedInstance(instance, persistInScene: true, destroyAfterSeconds: 0f);
            instance.SetActive(_visible);
            return instance;
        }

        void DismissStrikeTelegraph(int strikeIndex)
        {
            if (strikeIndex < 0 || strikeIndex >= _strikes.Count) return;
            StrikeSlot slot = _strikes[strikeIndex];
            if (slot.Telegraph != null)
            {
                Object.Destroy(slot.Telegraph);
                slot.Telegraph = null;
                _strikes[strikeIndex] = slot;
            }
        }

        void StopSpawnRoutine()
        {
            if (_spawnRoutine != null && _coroutineHost != null)
                _coroutineHost.StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        GameObject ResolveTelegraphPrefab()
        {
            if (_catalog != null && _catalog.LakiStrikeTelegraph != null)
                return _catalog.LakiStrikeTelegraph;
            return _telegraphPrefabFallback;
        }

        static int PickRandomTile(int tileCount, HashSet<int> used, System.Random rng)
        {
            for (int attempt = 0; attempt < tileCount * 2; attempt++)
            {
                int candidate = rng.Next(0, tileCount);
                if (!used.Contains(candidate)) return candidate;
            }
            for (int t = 0; t < tileCount; t++)
            {
                if (!used.Contains(t)) return t;
            }
            return -1;
        }

        bool IsPlayerInsideAnyStrike(ArenaPosReference arenaReference)
        {
            if (_strikes.Count == 0) return false;
            for (int i = 0; i < _strikes.Count; i++)
            {
                if (DoesStrikeHitPlayer(arenaReference, _strikes[i], GetLiveStrikeHitRadius(_strikes[i]), out _)) return true;
            }
            return false;
        }

        static bool DoesStrikeHitPlayer(
            ArenaPosReference arenaReference,
            StrikeSlot strike,
            float hitRadius,
            out string reason)
        {
            Vector3 playerPos = ResolvePlayerWorldPosition();
            Vector3 d = playerPos - strike.Center;
            d.y = 0f;
            float dist = d.magnitude;
            bool hit = dist <= hitRadius;
            reason = hit
                ? $"radius dist={dist:F2}m <= {hitRadius:F2}m (player={playerPos}) center={strike.Center}"
                : $"radius dist={dist:F2}m > {hitRadius:F2}m (player={playerPos}) center={strike.Center}";
            return hit;
        }

        static Vector3 ResolvePlayerWorldPosition()
        {
            INaraController nara = ResolveNaraController(null);
            if (nara != null && nara.NaraViewGO != null)
                return nara.NaraViewGO.transform.position;
            var naraView = Object.FindFirstObjectByType<NaraView>();
            return naraView != null ? naraView.transform.position : Vector3.zero;
        }

        static IEffectable ResolvePlayerEffectable(ArenaPosReference arenaReference)
        {
            INaraController nara = ResolveNaraController(arenaReference);
            return nara as IEffectable;
        }

        static INaraController ResolveNaraController(ArenaPosReference arenaReference)
        {
            if (arenaReference != null && arenaReference.NaraController != null)
                return arenaReference.NaraController;

            try
            {
                var sceneCtxs = Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneCtxs.Length; i++)
                {
                    var sc = sceneCtxs[i];
                    if (sc == null || sc.Container == null) continue;
                    try { return sc.Container.Resolve<INaraController>(); } catch { }
                }
            }
            catch { }

            return null;
        }

        static float ResolveGroundY(float fallbackY)
        {
            var naraView = Object.FindFirstObjectByType<NaraView>();
            if (naraView != null) return naraView.transform.position.y + GroundYOffset;
            return fallbackY + GroundYOffset;
        }

        static void ApplyTelegraphLayering(GameObject instance)
        {
            var layering = TelegraphLayeringLocator.Service;
            if (layering == null || instance == null) return;
            var layer = layering.Register(preferTop: true);
            var mrs = instance.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < mrs.Length; i++)
            {
                if (mrs[i] == null) continue;
                var mat = mrs[i].material;
                if (mat != null) mat.renderQueue += layer.QueueAdd;
            }
        }

        static void StripTransientAutoDestroy(GameObject instance)
        {
            if (instance == null) return;
            var transients = instance.GetComponentsInChildren<SkillCastTransientVfx>(true);
            for (int i = 0; i < transients.Length; i++)
            {
                if (transients[i] != null) Object.Destroy(transients[i]);
            }
        }

        void ClearTelegraphObjects()
        {
            for (int i = 0; i < _strikes.Count; i++)
            {
                StrikeSlot slot = _strikes[i];
                if (slot.Telegraph != null) Object.Destroy(slot.Telegraph);
            }
        }
    }
}
