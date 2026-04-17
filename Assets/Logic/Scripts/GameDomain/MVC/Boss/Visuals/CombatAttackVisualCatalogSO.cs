using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Visuals
{
    [CreateAssetMenu(fileName = "CombatAttackVisualCatalog", menuName = "Scriptable Objects/Boss/Combat Attack Visual Catalog")]
    public class CombatAttackVisualCatalogSO : ScriptableObject
    {
        public const int LakiRouletteTileTypes = 3;

        /// <summary>
        /// Telegraph = aviso antes do golpe. Area = opcional; pode ser VFX de impacto na resolução do hit (Particle System, mesh curta, etc.) — o spawn costuma ser feito no handler na hora do acerto.
        /// </summary>
        [System.Serializable]
        public struct DisplacementAttackVisuals
        {
            public GameObject NormalTelegraphPrefab;
            public GameObject PullTelegraphPrefab;
            public GameObject PushTelegraphPrefab;
            public GameObject NormalAreaPrefab;
            public GameObject PullAreaPrefab;
            public GameObject PushAreaPrefab;
        }

        /// <summary>Telegraph de coluna/faixa + VFX de impacto opcional (Normal/Pull/Push), alinhado ao deslocamento do ataque.</summary>
        [System.Serializable]
        public struct FeatherColumnVisuals
        {
            public GameObject ColumnNormalPrefab;
            public GameObject ColumnPullPrefab;
            public GameObject ColumnPushPrefab;
            public GameObject ColumnNormalAreaPrefab;
            public GameObject ColumnPullAreaPrefab;
            public GameObject ColumnPushAreaPrefab;
        }

        [Header("Hokari — combat (telegraph + optional impact VFX)")]
        [Tooltip("ProteanCones + BigCones enum ids.")]
        [SerializeField] private DisplacementAttackVisuals _proteanCones;
        [Tooltip("WingSlash + BigWindSlash.")]
        [SerializeField] private DisplacementAttackVisuals _wingSlash;
        [Tooltip("SkySwords + SkySwordsG/K + BigSkySwordsG/K.")]
        [SerializeField] private DisplacementAttackVisuals _skySwords;
        [SerializeField] private DisplacementAttackVisuals _circle;
        [Tooltip("Todos os ataques de penas em linha (X/Z/XZ, G/K).")]
        [SerializeField] private DisplacementAttackVisuals _featherLines;
        [Tooltip("Telegraph prefabs (Normal/Pull/Push): o selecionado vira a área visual ao redor do orb em OrbView (escala x/z = raio). O prefab com OrbController continua em BossAttack → Orb.")]
        [SerializeField] private DisplacementAttackVisuals _orb;
        [Tooltip("Idem quando BossAttack.VisualId = BigOrb; senão usa a linha Orb acima.")]
        [SerializeField] private DisplacementAttackVisuals _bigOrb;

        [Header("Feather columns (telegraph de faixa / coluna)")]
        [SerializeField] private FeatherColumnVisuals _featherColumns;

        [Header("Laki roulette — inner ring (index = TileEffectType: Neutral, Positive, Negative)")]
        [SerializeField] private GameObject[] _lakiRouletteInnerTilePrefabs = new GameObject[LakiRouletteTileTypes];
        [Header("Laki roulette — outer ring (same index order)")]
        [SerializeField] private GameObject[] _lakiRouletteOuterTilePrefabs = new GameObject[LakiRouletteTileTypes];

        public DisplacementAttackVisuals ProteanCones => _proteanCones;
        public DisplacementAttackVisuals WingSlash => _wingSlash;
        public DisplacementAttackVisuals SkySwords => _skySwords;
        public DisplacementAttackVisuals Circle => _circle;
        public DisplacementAttackVisuals FeatherLines => _featherLines;
        public DisplacementAttackVisuals Orb => _orb;
        public DisplacementAttackVisuals BigOrb => _bigOrb;
        public FeatherColumnVisuals FeatherColumns => _featherColumns;

        public GameObject GetTelegraph(HokariBossAttackVisualId id, bool isPull, bool isPush)
        {
            return ResolveTelegraph(ResolveVisuals(id), isPull, isPush);
        }

        public GameObject GetArea(HokariBossAttackVisualId id, bool isPull, bool isPush)
        {
            return ResolveArea(ResolveVisuals(id), isPull, isPush);
        }

        public GameObject GetFeatherColumnPrefab(bool isPull, bool isPush)
        {
            if (isPull && _featherColumns.ColumnPullPrefab != null) return _featherColumns.ColumnPullPrefab;
            if (isPush && _featherColumns.ColumnPushPrefab != null) return _featherColumns.ColumnPushPrefab;
            return _featherColumns.ColumnNormalPrefab;
        }

        public GameObject GetFeatherColumnAreaPrefab(bool isPull, bool isPush)
        {
            if (isPull && _featherColumns.ColumnPullAreaPrefab != null) return _featherColumns.ColumnPullAreaPrefab;
            if (isPush && _featherColumns.ColumnPushAreaPrefab != null) return _featherColumns.ColumnPushAreaPrefab;
            return _featherColumns.ColumnNormalAreaPrefab;
        }

        public bool TryGetLakiRouletteTilePrefab(bool isInnerBand, RouletteArenaService.TileEffectType type, out GameObject prefab)
        {
            prefab = null;
            int ti = (int)type;
            if (ti < 0 || ti >= LakiRouletteTileTypes) return false;
            var arr = isInnerBand ? _lakiRouletteInnerTilePrefabs : _lakiRouletteOuterTilePrefabs;
            if (arr == null || ti >= arr.Length) return false;
            prefab = arr[ti];
            return prefab != null;
        }

        private DisplacementAttackVisuals ResolveVisuals(HokariBossAttackVisualId id) => id switch
        {
            HokariBossAttackVisualId.ProteanCones or HokariBossAttackVisualId.BigCones => _proteanCones,
            HokariBossAttackVisualId.WingSlash or HokariBossAttackVisualId.BigWindSlash => _wingSlash,
            HokariBossAttackVisualId.SkySwords or HokariBossAttackVisualId.SkySwordsG or HokariBossAttackVisualId.SkySwordsK
                or HokariBossAttackVisualId.BigSkySwordsG or HokariBossAttackVisualId.BigSkySwordsK => _skySwords,
            HokariBossAttackVisualId.Circle => _circle,
            HokariBossAttackVisualId.XFeatherG or HokariBossAttackVisualId.XFeatherK or HokariBossAttackVisualId.ZFeatherG
                or HokariBossAttackVisualId.ZFeatherK or HokariBossAttackVisualId.XZFeatherG or HokariBossAttackVisualId.XZFeatherK => _featherLines,
            HokariBossAttackVisualId.Orb => _orb,
            HokariBossAttackVisualId.BigOrb => _bigOrb,
            _ => default,
        };

        private static GameObject ResolveTelegraph(in DisplacementAttackVisuals v, bool isPull, bool isPush)
        {
            if (isPull && v.PullTelegraphPrefab != null) return v.PullTelegraphPrefab;
            if (isPush && v.PushTelegraphPrefab != null) return v.PushTelegraphPrefab;
            return v.NormalTelegraphPrefab;
        }

        private static GameObject ResolveArea(in DisplacementAttackVisuals v, bool isPull, bool isPush)
        {
            if (isPull && v.PullAreaPrefab != null) return v.PullAreaPrefab;
            if (isPush && v.PushAreaPrefab != null) return v.PushAreaPrefab;
            return v.NormalAreaPrefab;
        }
    }
}
