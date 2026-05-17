using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Environment;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari
{
    [CreateAssetMenu(fileName = "HokariArenaHazardPattern", menuName = "ScriptableObjects/Environment/Hokari Arena Hazard Pattern")]
    public sealed class HokariArenaHazardPatternSO : ScriptableObject
    {
        [Tooltip("Used when a definition leaves Telegraph Disc Radius at 0.")]
        [Min(0.1f)] public float DefaultTelegraphDiscRadius = 3.5f;

        [Header("Hazard pool (random pick per turn among matching entries)")]
        [SerializeField] private HokariArenaHazardDefinitionSO[] _definitions = System.Array.Empty<HokariArenaHazardDefinitionSO>();

        public IReadOnlyList<HokariArenaHazardDefinitionSO> Definitions => _definitions ?? System.Array.Empty<HokariArenaHazardDefinitionSO>();

        public float ResolveTelegraphDiscRadius(HokariArenaHazardDefinitionSO definition)
        {
            if (definition != null && definition.TelegraphDiscRadius > 0f)
                return definition.TelegraphDiscRadius;
            return Mathf.Max(0.1f, DefaultTelegraphDiscRadius);
        }

        void OnValidate()
        {
            if (_definitions == null) return;
            for (int i = 0; i < _definitions.Length; i++)
                _definitions[i]?.SyncCatalogAndPushFromDisplacementKind();
        }

        public int CollectMatching(int turnNumber, List<HokariArenaHazardDefinitionSO> results)
        {
            results?.Clear();
            if (_definitions == null || results == null) return 0;
            for (int i = 0; i < _definitions.Length; i++)
            {
                var def = _definitions[i];
                if (def != null && def.MatchesTurn(turnNumber))
                    results.Add(def);
            }
            return results.Count;
        }

        public bool HasAnyForTurn(int turnNumber)
        {
            if (_definitions == null) return false;
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (_definitions[i] != null && _definitions[i].MatchesTurn(turnNumber))
                    return true;
            }
            return false;
        }

        public HokariArenaHazardDefinitionSO PickRandomForTurn(int turnNumber)
        {
            var pool = new List<HokariArenaHazardDefinitionSO>(16);
            CollectMatching(turnNumber, pool);
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        [System.Obsolete("Use PickRandomForTurn")]
        public bool TryGetEntryForTurn(int turnNumber, out HokariArenaHazardTurnEntry entry)
        {
            entry = default;
            var def = PickRandomForTurn(turnNumber);
            if (def == null) return false;
            entry = new HokariArenaHazardTurnEntry
            {
                TurnMin = def.TurnMin,
                TurnMax = def.TurnMax,
                Push = def.Push,
                ApplyToBook = def.ApplyToBook,
                DelayBeforePushSeconds = def.DelayBeforePushSeconds,
            };
            return true;
        }
    }

    [System.Serializable]
    public struct HokariArenaHazardTurnEntry
    {
        [Min(0)] public int TurnMin;
        [Min(0)] public int TurnMax;
        public PlanarPushRequest Push;
        public bool ApplyToBook;
        [Min(0f)] public float DelayBeforePushSeconds;
    }
}
