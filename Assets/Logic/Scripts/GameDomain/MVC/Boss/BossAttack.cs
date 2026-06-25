using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Cone;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Feather;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Orb;
using Logic.Scripts.GameDomain.Commands;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using Logic.Scripts.GameDomain.MVC.Environment;
using Logic.Scripts.GameDomain.MVC.Boss.AttackGizmos;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Feather;
using Zenject;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Logic.Scripts.GameDomain.MVC.Boss
{
    public class BossAttack : MonoBehaviour
    {
        [SerializeReference] private List<AbilityEffect> _effects;

        private enum AttackType
        {
            ProteanCones = 0,
            FeatherLines = 1,
            WingSlash = 2,
            Orb = 3,
            HookAwakening = 4,
            SkySwords = 5,
            Minigame = 6,
            Circle = 7,
            Deprecated_PlayerFootCircle = 8,
            DiceAttack = 9,
            LakiArenaTileTelegraph = 10
        }
        [SerializeField] private AttackType _attackType = AttackType.ProteanCones;

        [SerializeField] private int _displacementPriority = 0;
        private bool _displacementEnabled = true;
        private bool _telegraphDisplacementEnabled = true;

        [SerializeField] private ProteanConesParams _protean = new ProteanConesParams { radius = 3f, angleDeg = 60f, sides = 36 };
        [SerializeField] private ProteanConesParams _wingSlash = new ProteanConesParams { radius = 4f, angleDeg = 215f, sides = 48 };

        [SerializeField] private FeatherLinesParams _feather = new FeatherLinesParams { featherCount = 3, axisMode = FeatherAxisMode.XZ, stripHalfExtent = 0f, telegraphStripUniformScale = 1f, width = 2f, margin = 5f, forceBase = 2f, forcePerMeter = 0.4f, forcePerDebuff = 0.5f };

        [Header("Feather Visuals")]
        [SerializeField] private bool _featherIsPull = false;

        [System.Serializable]
        private struct OrbSpawnParams
        {
            public GameObject prefab;
            public float moveStepMeters;
            public float growStepMeters;
            public float initialRadius;
            public float maxRadiusCap;
            public int baseDamage;
            public int initialHp;
        }

        [SerializeField] private OrbSpawnParams _orb = new OrbSpawnParams { prefab = null, moveStepMeters = 6f, growStepMeters = 6f, initialRadius = 4f, maxRadiusCap = 60f, baseDamage = 10, initialHp = 50 };

        [System.Serializable]
        private struct SkySwordsParams
        {
            public float radius;
            public float ringWidth;
        }

        [System.Serializable]
        private struct CircleParams
        {
            public float radius;
            public float ringWidth;
        }

        [Header("Sky Swords")]
        [SerializeField] private SkySwordsParams _skySwords = new SkySwordsParams { radius = 4.5f, ringWidth = 0.3f };
        [SerializeField] private bool _skySwordsIsPull = false;

        [Header("Circle AoE")]
        [SerializeField] private CircleParams _circle = new CircleParams { radius = 3.5f, ringWidth = 0.25f };

        [Header("Combat visual catalog (Hokari)")]
        [Tooltip("Row in CombatAttackVisualCatalog. None = infer from Attack Type (Protean / Wing / Sky / Circle). Normal/Pull/Push telegraphs follow Grapple & Knockback on this attack.")]
        [FormerlySerializedAs("_hocariAttackVisualId")]
        [SerializeField] private HokariBossAttackVisualId _hokariAttackVisualId;

        [Tooltip("Distance from arena center to edge (meters). Catalog meshes use scale 1 = full arena: SkySwords disc XZ scale = SkySwords.radius / this; Feather strip half-length uses FeatherLines.stripHalfExtent when > 0, else this.")]
        [SerializeField] private float _hokariArenaTelegraphHalfExtent = 10f;

        private ArenaPosReference _arena;
        private IEffectable _caster;
        private IBossAttackHandler _handler;
        private bool _executing;
        private System.Threading.Tasks.TaskCompletionSource<bool> _executeTcs;
        private ICommandFactory _commandFactory;
        [Zenject.Inject(Optional = true)] private Logic.Scripts.GameDomain.MVC.Boss.Telegraph.ITelegraphMaterialProvider _telegraphProvider;
        [Zenject.Inject(Optional = true)] private CombatAttackVisualCatalogSO _attackVisualCatalog;
        private CombatAttackVisualCatalogSO _resolvedCombatAttackVisualCatalog;

        private IAudioService _audio;
        private int _lakiTileTelegraphSelectionSalt;

        [Header("Laki Minigame (legacy)")]
        public GameObject _minigameRoundPrefab;
        [Header("Dice Attack (Laki — no round prefab)")]
        [SerializeField] private string _diceAttackDisplayName = "DiceAttack";
        [SerializeField] private GameObject _diceAttackPlayerDiePrefab;
        [SerializeField] private GameObject _diceAttackBossDiePrefab;
        [SerializeField] private int _diceAttackDieHp = 99;
        [SerializeField] private float _diceAttackPlayerRollInputConsumeDelay = 0.1f;
        [SerializeField] private GameObject _diceAttackPlayerRollPromptPrefab;

        [System.Serializable]
        private struct LakiArenaTileTelegraphParams
        {
            [Min(1), Tooltip("How many circular telegraphs to place on tile centers.")]
            public int AreaCount;
            [Range(0f, 1f), Tooltip("Per-area chance to target the tile the player is standing on.")]
            public float PlayerTileChance;
            [Min(0.1f), Tooltip("Scales the telegraph prefab root (X/Z). VFX children keep local scale (e.g. 0.15 on the VFX child).")]
            public float TelegraphDiscRadius;
            [Min(0.01f), Tooltip("World hit radius in meters when Telegraph Disc Radius = 1. Match what you see in play (e.g. 6 for a ~6m disc at radius 1).")]
            public float HitRadiusMetersAtUnitDisc;
            [Min(0f), Tooltip("Extra meters added to the computed hit radius.")]
            public float HitRadiusPadding;
            [Min(0f), Tooltip("Seconds between spawning each telegraph disc.")]
            public float TelegraphSpawnInterval;
            [Min(0f), Tooltip("Seconds between resolving damage for each area.")]
            public float StrikeResolveInterval;
        }

        [Header("Laki arena — tile telegraphs")]
        [SerializeField] private LakiArenaTileTelegraphParams _lakiArenaTileTelegraph = new LakiArenaTileTelegraphParams
        {
            AreaCount = 2,
            PlayerTileChance = 0.35f,
            TelegraphDiscRadius = 1f,
            HitRadiusMetersAtUnitDisc = 3f,
            HitRadiusPadding = 0f,
            TelegraphSpawnInterval = 0.35f,
            StrikeResolveInterval = 0.25f,
        };

        public float GetLakiArenaTileTelegraphDiscRadius() =>
            Mathf.Max(0.1f, _lakiArenaTileTelegraph.TelegraphDiscRadius);

        public float GetLakiArenaTileTelegraphHitRadiusMeters()
        {
            var p = _lakiArenaTileTelegraph;
            float perUnit = p.HitRadiusMetersAtUnitDisc > 0.01f ? p.HitRadiusMetersAtUnitDisc : 3f;
            return GetLakiArenaTileTelegraphDiscRadius() * perUnit + Mathf.Max(0f, p.HitRadiusPadding);
        }

        public int GetDisplacementPriority() { return _displacementPriority; }
        public void SetDisplacementEnabled(bool enabled) { _displacementEnabled = enabled; }
        public void ConfigureTelegraphDisplacementEnabled(bool enabled) { _telegraphDisplacementEnabled = enabled; }
        private static bool IsForcedMovementEffect(AbilityEffect fx)
        {
            if (fx == null) return false;
            if (fx is Assets.Logic.Scripts.GameDomain.Effects.DisplacementEffect) return true;
            if (fx is Logic.Scripts.GameDomain.Effects.KnockbackEffect) return true;
            if (fx is Logic.Scripts.GameDomain.Effects.GrappleEffect) return true;
            return false;
        }

        public bool HasDisplacementEffect()
        {
            if (_effects == null) return false;
            for (int i = 0; i < _effects.Count; i++)
            {
                if (IsForcedMovementEffect(_effects[i])) return true;
            }
            return false;
        }
        public bool IsMinigameAttack() => _attackType == AttackType.Minigame;
        public bool IsDiceAttack() => _attackType == AttackType.DiceAttack;
        public bool IsLakiArenaTileTelegraph() => _attackType == AttackType.LakiArenaTileTelegraph;

        public void SetLakiTileTelegraphSelectionSalt(int salt) => _lakiTileTelegraphSelectionSalt = salt;

        int GetLakiTileTelegraphSelectionSeed()
        {
            int areaSalt = _attackType == AttackType.LakiArenaTileTelegraph
                ? Mathf.Max(1, _lakiArenaTileTelegraph.AreaCount)
                : 0;
            return (GetInstanceID() * 7919) ^ (_lakiTileTelegraphSelectionSalt * 104729) ^ areaSalt;
        }
        public int GetAnimationId() { return (int)_attackType; }
        public object GetAttackTypeBoxed() { return _attackType; } // for external mapping without exposing enum type

        public string GetAttackTypeName()
        {
            return _attackType.ToString();
        }
        public void StripDisplacementForTelegraph()
        {
            if (_effects == null || _effects.Count == 0) return;
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                if (IsForcedMovementEffect(_effects[i]))
                {
                    _effects.RemoveAt(i);
                }
            }
        }

        public void Setup(ArenaPosReference arena, IEffectable caster)
        {
            _arena = arena;
            _caster = caster;
            try { _commandFactory = ProjectContext.Instance.Container.Resolve<ICommandFactory>(); } catch { _commandFactory = null; }
            try { _audio = ProjectContext.Instance.Container.Resolve<IAudioService>(); } catch { _audio = null; }
            if (_attackVisualCatalog == null) _attackVisualCatalog = Resources.Load<CombatAttackVisualCatalogSO>("CombatAttackVisualCatalog");
#if UNITY_EDITOR
            if (_attackVisualCatalog == null) {
                _attackVisualCatalog = AssetDatabase.LoadAssetAtPath<CombatAttackVisualCatalogSO>(
                    "Assets/Logic/Scripts/GameDomain/MVC/Boss/Visuals/CombatAttackVisualCatalog.asset");
            }
#endif

            SelectAndBuildHandler();
            Transform parentForTelegraph = transform;
            if (_attackType == AttackType.FeatherLines)
            {
                parentForTelegraph = _arena != null ? _arena.transform : transform;
                Logic.Scripts.GameDomain.MVC.Boss.Attacks.Feather.FeatherLinesHandler.PrimeNextTelegraphDisplacementEnabled(_telegraphDisplacementEnabled);
            }
            else if (_attackType == AttackType.SkySwords)
            {
                bool hasGrapple = false;
                bool hasKnock = false;
                if (_effects != null)
                {
                    for (int i = 0; i < _effects.Count; i++)
                    {
                        var fx = _effects[i];
                        if (fx == null) continue;
                        if (fx is Logic.Scripts.GameDomain.Effects.GrappleEffect) hasGrapple = true;
                        else if (fx is Logic.Scripts.GameDomain.Effects.KnockbackEffect) hasKnock = true;
                    }
                }
                if (hasGrapple)
                {
                    Logic.Scripts.GameDomain.MVC.Boss.Attacks.SkySwords.SkySwordsHandler.PrimeNextTelegraphPull(true);
                }
                else if (hasKnock)
                {
                    Logic.Scripts.GameDomain.MVC.Boss.Attacks.SkySwords.SkySwordsHandler.PrimeNextTelegraphPull(false);
                }
            }
            _handler?.PrepareTelegraph(parentForTelegraph);
            // Prepare hidden; controller reveals mid-prep if handler supports it
            TrySetTelegraphVisible(false);
        }

        public void Execute()
        {
            if (_attackType != AttackType.FeatherLines)
                _audio?.PlaySfx(SfxIds.Hocari_Ataque_Laminas, AudioChannelType.SfxBoss);

            if (_attackType == AttackType.Orb)
            {
                if (_executeTcs == null) _executeTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                if (_executing) return;
                _executing = true;
                TrySpawnOrb();
                CleanupAndComplete();
                return;
            }
            if (_handler == null) { Destroy(gameObject); return; }
            if (_executeTcs == null) _executeTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            if (_executing) return;
            _executing = true;
            bool hit = _handler.ComputeHits(_arena, transform, _caster);
            StartCoroutine(ExecuteAndCleanup());
        }

        public System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_attackType != AttackType.FeatherLines)
                _audio?.PlaySfx(SfxIds.Hocari_Ataque_Laminas, AudioChannelType.SfxBoss);

            if (_attackType == AttackType.Orb)
            {
                if (_executeTcs == null) _executeTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                if (!_executing)
                {
                    _executing = true;
                    TrySpawnOrb();
                    CleanupAndComplete();
                }
                return _executeTcs.Task;
            }
            if (_attackType == AttackType.DiceAttack)
            {
                if (_executeTcs == null) _executeTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                if (!_executing)
                {
                    _executing = true;
                    TryStartDiceAttack();
                    CleanupAndComplete();
                }
                return _executeTcs.Task;
            }
            if (_attackType == AttackType.Minigame)
            {
                if (_executeTcs == null) _executeTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                if (!_executing)
                {
                    _executing = true;
                    TryStartMinigameRound();
                    CleanupAndComplete();
                }
                return _executeTcs.Task;
            }
            if (_handler == null) { return System.Threading.Tasks.Task.CompletedTask; }
            if (_executeTcs == null) _executeTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            if (!_executing)
            {
                _executing = true;
                bool hit = _handler.ComputeHits(_arena, transform, _caster);
                StartCoroutine(ExecuteAndCleanup());
            }
            return _executeTcs.Task;
        }

        private System.Collections.IEnumerator ExecuteAndCleanup()
        {
            if (_effects != null)
            {
                System.Collections.Generic.List<AbilityEffect> effectsToRun = _effects;
                bool stripForcedMove = !_displacementEnabled
                    || _attackType == AttackType.WingSlash
                    || CombatArenaBoundaryRuntime.Policy == CombatArenaDispositionPolicy.HokariVoluntaryPlusRingOut;
                if (stripForcedMove && _effects != null)
                {
                    var filtered = new System.Collections.Generic.List<AbilityEffect>(_effects.Count);
                    foreach (var fx in _effects)
                    {
                        if (!IsForcedMovementEffect(fx))
                            filtered.Add(fx);
                    }
                    effectsToRun = filtered;
                }
                yield return _handler.ExecuteEffects(effectsToRun, _arena, transform, _caster);
            }
            CleanupAndComplete();
        }

        private void CleanupAndComplete()
        {
            _handler?.Cleanup();
            Destroy(gameObject);
            if (_executeTcs != null && !_executeTcs.Task.IsCompleted) _executeTcs.TrySetResult(true);
        }

        private void TrySpawnOrb()
        {
            if (_orb.prefab == null)
            {
                Debug.LogWarning("BossAttack Orb: prefab is null");
                return;
            }
            if (_commandFactory != null)
            {
                var spawnByFactory = _commandFactory.CreateCommandVoid<SpawnOrbCommand>();
                Logic.Scripts.GameDomain.MVC.Environment.Orb.OrbRegistry reg = null;
                try { reg = ProjectContext.Instance.Container.Resolve<Logic.Scripts.GameDomain.MVC.Environment.Orb.OrbRegistry>(); } catch { reg = null; }
                Vector3 origin = transform.position;
                var bossView = GetComponentInParent<Logic.Scripts.GameDomain.MVC.Boss.BossView>();
                if (bossView != null) origin = bossView.transform.position;
                spawnByFactory.SetData(new SpawnOrbData
                {
                    Arena = _arena,
                    Origin = origin,
                    Prefab = _orb.prefab,
                    Registry = reg,
                    MoveStep = _orb.moveStepMeters,
                    GrowStep = _orb.growStepMeters,
                    InitialRadius = _orb.initialRadius,
                    MaxRadius = _orb.maxRadiusCap,
                    BaseDamage = _orb.baseDamage,
                    InitialHp = _orb.initialHp,
                    OrbAreaVisualPrefab = ResolveOrbAreaVisualPrefabFromCatalog()
                });
                UnityEngine.Debug.Log($"[BossAttack][Orb] Spawning via CommandFactory at {transform.position}");
                spawnByFactory.Execute();
                return;
            }
            var spawn = new SpawnOrbCommand();
            spawn.ResolveDependencies();
            spawn.SetData(new SpawnOrbData
            {
                Arena = _arena,
                Origin = transform.position,
                Prefab = _orb.prefab,
                MoveStep = _orb.moveStepMeters,
                GrowStep = _orb.growStepMeters,
                InitialRadius = _orb.initialRadius,
                MaxRadius = _orb.maxRadiusCap,
                BaseDamage = _orb.baseDamage,
                InitialHp = _orb.initialHp,
                OrbAreaVisualPrefab = ResolveOrbAreaVisualPrefabFromCatalog()
            });
            UnityEngine.Debug.Log($"[BossAttack][Orb] Spawning via fallback at {transform.position}");
            spawn.Execute();
        }

        private void SelectAndBuildHandler()
        {
            Material meshMat = ResolveTelegraphMeshMaterial();
            Material lineBase = ResolveTelegraphLineMaterialFor(false);
            Material lineDisp = ResolveTelegraphLineMaterialFor(true);
            Material meshBase = ResolveTelegraphMeshMaterialFor(false);
            Material meshDisp = ResolveTelegraphMeshMaterialFor(true);
            // UnityEngine.Debug.Log($"[BossAttack] Using telegraph materials | mesh={(meshMat != null ? meshMat.name : "NULL")} lineBase={(lineBase != null ? lineBase.name : "NULL")} lineDisp={(lineDisp != null ? lineDisp.name : "NULL")}");
            switch (_attackType)
            {
                case AttackType.ProteanCones:
                {
                    float[] yaws = new float[] { 0f, 90f, 180f, 270f };
                    _handler = new ConeAttackHandler(_protean.radius, _protean.angleDeg, _protean.sides, yaws, lineBase ?? meshBase, meshBase, ResolveVisualPrefabForDisplacement(false), ResolveConeTelegraphUniformScale());
                    break;
                }
                case AttackType.Circle:
                {
                    _handler = new Logic.Scripts.GameDomain.MVC.Boss.Attacks.Circle.CircleAttackHandler(
                        _circle.radius,
                        _circle.ringWidth,
                        lineBase ?? meshBase,
                        meshBase,
                        ResolveVisualPrefabForDisplacement(false));
                    break;
                }
                case AttackType.Deprecated_PlayerFootCircle:
                {
                    Debug.LogWarning("[BossAttack] Deprecated_PlayerFootCircle is no longer supported — use Circle. Attack will not run.");
                    _handler = null;
                    break;
                }
                case AttackType.FeatherLines:
                {
                    GameObject colN = null, colPull = null, colPush = null;
                    GameObject telN = null, telPull = null, telPush = null;
                    var catalog = GetCombatAttackVisualCatalog();
                    if (catalog != null)
                    {
                        colN = catalog.GetFeatherColumnPrefab(false, false);
                        colPull = catalog.GetFeatherColumnPrefab(true, false);
                        colPush = catalog.GetFeatherColumnPrefab(false, true);
                        var featherVid = ResolveCatalogVisualId();
                        if (featherVid != HokariBossAttackVisualId.None)
                        {
                            telN = catalog.GetTelegraph(featherVid, false, false);
                            telPull = catalog.GetTelegraph(featherVid, true, false);
                            telPush = catalog.GetTelegraph(featherVid, false, true);
                        }
                    }
                    var featherParams = _feather;
                    if (featherParams.stripHalfExtent <= 0f)
                        featherParams.stripHalfExtent = _hokariArenaTelegraphHalfExtent;
                    _handler = new FeatherLinesHandler(featherParams, _featherIsPull, lineBase ?? meshBase, lineDisp ?? meshDisp, meshBase, meshDisp, colN, colPull, colPush, telN, telPull, telPush);
                    break;
                }
                case AttackType.WingSlash:
                {
                    float angleAbs = Mathf.Abs(_wingSlash.angleDeg);
                    // Escolhe o lado dinamicamente igual à animação (cross entre forward da Hokari e vetor até o player)
                    float yawBase = -90f;
                    try
                    {
                        Vector3 player = Vector3.zero;
                        if (_arena != null)
                            player = _arena.RelativeArenaPositionToRealPosition(_arena.GetPlayerArenaPosition());
                        else
                        {
                            var naraView = Object.FindFirstObjectByType<Logic.Scripts.GameDomain.MVC.Nara.NaraView>(FindObjectsInactive.Exclude);
                            if (naraView != null) player = naraView.transform.position;
                        }
                        var bossTr = GetComponentInParent<Logic.Scripts.GameDomain.MVC.Boss.BossView>()?.transform ?? transform;
                        Vector3 toPlayer = player - bossTr.position; toPlayer.y = 0f;
                        Vector3 fwd = bossTr.forward; fwd.y = 0f;
                        if (toPlayer.sqrMagnitude > 1e-6f && fwd.sqrMagnitude > 1e-6f)
                        {
                            toPlayer.Normalize(); fwd.Normalize();
                            float crossY = Vector3.Cross(fwd, toPlayer).y;
                            // Inverte o sinal para alinhar o cone do telegraph com a animação observada
                            yawBase = (crossY >= 0f) ? -90f : 90f;
                        }
                    }
                    catch { yawBase = -90f; }
                    float[] yaws = new float[] { yawBase };
                    _handler = new ConeAttackHandler(_wingSlash.radius, angleAbs, _wingSlash.sides, yaws, lineBase ?? meshBase, meshBase, ResolveVisualPrefabForDisplacement(_telegraphDisplacementEnabled), ResolveConeTelegraphUniformScale());
                    break;
                }
                case AttackType.Orb:
                {
                    _handler = new OrbSpawnHandler(_orb.initialRadius);
                    break;
                }
                case AttackType.SkySwords:
                {
                    // Materiais específicos (line/mesh) resolvidos com displacement flag (para Grapple/Knockback/Normal)
                    Material ssLine = ResolveTelegraphLineMaterialFor(_telegraphDisplacementEnabled);
                    Material ssMesh = ResolveTelegraphMeshMaterialFor(_telegraphDisplacementEnabled);
                    const float skyDiscScale = 0.3f;
                    _handler = new Logic.Scripts.GameDomain.MVC.Boss.Attacks.SkySwords.SkySwordsHandler(
                        _skySwords.radius,
                        _skySwords.ringWidth,
                        _skySwordsIsPull,
                        _telegraphDisplacementEnabled,
                        ssLine,
                        ssMesh,
                        ResolveVisualPrefabForDisplacement(_telegraphDisplacementEnabled),
                        skyDiscScale);
                    break;
                }
                case AttackType.LakiArenaTileTelegraph:
                {
                    _handler = new Logic.Scripts.GameDomain.MVC.Boss.Attacks.Laki.LakiArenaTileTelegraphAttackHandler(
                        _lakiArenaTileTelegraph.AreaCount,
                        _lakiArenaTileTelegraph.PlayerTileChance,
                        _lakiArenaTileTelegraph.TelegraphDiscRadius,
                        _lakiArenaTileTelegraph.HitRadiusMetersAtUnitDisc,
                        _lakiArenaTileTelegraph.HitRadiusPadding,
                        _lakiArenaTileTelegraph.TelegraphSpawnInterval,
                        _lakiArenaTileTelegraph.StrikeResolveInterval,
                        GetCombatAttackVisualCatalog(),
                        null,
                        this,
                        GetLakiTileTelegraphSelectionSeed());
                    break;
                }
                default:
                {
                    _handler = null;
                    break;
                }
            }
        }

        private void TryStartMinigameRound()
        {
            GameObject prefab = _minigameRoundPrefab;
            if (prefab == null)
            {
                var binder = GetComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.LakiMinigameAttackBinder>();
                if (binder != null) prefab = binder.RoundPrefab;
                if (prefab == null)
                {
                    Debug.LogWarning("[BossAttack][Minigame] Prefab is null (set _minigameRoundPrefab or add LakiMinigameAttackBinder)");
                    return;
                }
            }
            var go = Instantiate(prefab);
            var round = go.GetComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.IMinigameRound>();
            if (round == null)
            {
                Debug.LogWarning("[BossAttack][Minigame] Prefab missing IMinigameRound component");
                Destroy(go);
                return;
            }
            try { Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.SetActiveName(round.MinigameName); } catch { }
            Logic.Scripts.Turns.TurnStateService turnSvc = null;
            Logic.Scripts.Turns.IEnvironmentActorsRegistry envReg = null;
            Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay bossRelay = null;
            Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView arenaView = null;
            Logic.Scripts.GameDomain.MVC.Nara.INaraController nara = null;
            Logic.Scripts.GameDomain.MVC.Boss.IBossController bossCtrl = null;
            Zenject.DiContainer sceneContainer = null;
            try {
                var sceneCtxs = Object.FindObjectsByType<Zenject.SceneContext>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneCtxs.Length; i++) {
                    var sc = sceneCtxs[i];
                    if (sc != null && sc.gameObject.scene == gameObject.scene) { sceneContainer = sc.Container; break; }
                }
            } catch { }
            try { if (sceneContainer != null) turnSvc = sceneContainer.Resolve<Logic.Scripts.Turns.TurnStateService>(); } catch { }
            try { if (sceneContainer != null) envReg = sceneContainer.Resolve<Logic.Scripts.Turns.IEnvironmentActorsRegistry>(); } catch { }
            try
            {
                var bossView = GetComponentInParent<Logic.Scripts.GameDomain.MVC.Boss.BossView>();
                bossRelay = bossView != null ? bossView.GetComponent<Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay>() : null;
            } catch { }
            try { arenaView = FindFirstObjectByType<Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView>(); } catch { }
            try { if (sceneContainer != null) nara = sceneContainer.Resolve<Logic.Scripts.GameDomain.MVC.Nara.INaraController>(); } catch { }
            try { if (sceneContainer != null) bossCtrl = sceneContainer.Resolve<Logic.Scripts.GameDomain.MVC.Boss.IBossController>(); } catch { }
            _ = round.StartAsync(turnSvc, envReg, bossRelay, arenaView, nara, bossCtrl);
        }

        private void TryStartDiceAttack()
        {
            if (Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.IsActive)
            {
                Debug.Log("[BossAttack][DiceAttack] Skipped — dice session already active.");
                return;
            }
            Zenject.DiContainer sceneContainer = null;
            try
            {
                var sceneCtxs = Object.FindObjectsByType<Zenject.SceneContext>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneCtxs.Length; i++)
                {
                    var sc = sceneCtxs[i];
                    if (sc != null && sc.gameObject.scene == gameObject.scene) { sceneContainer = sc.Container; break; }
                }
            }
            catch { }
            if (sceneContainer == null)
            {
                Debug.LogWarning("[BossAttack][DiceAttack] No SceneContext — cannot resolve player/boss.");
                return;
            }
            var settings = new Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackSettings
            {
                DisplayName = _diceAttackDisplayName,
                PlayerDiePrefab = _diceAttackPlayerDiePrefab,
                BossDiePrefab = _diceAttackBossDiePrefab,
                DieHp = _diceAttackDieHp,
                PlayerRollInputConsumeDelay = _diceAttackPlayerRollInputConsumeDelay,
                PlayerRollPromptPrefab = _diceAttackPlayerRollPromptPrefab
            };
            Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackSession.Start(settings, sceneContainer);
        }

        private Material ResolveTelegraphMaterial()
        {
            // Backwards compat: mesh
            return ResolveTelegraphMeshMaterial();
        }

        private Material ResolveTelegraphMaterialFor(bool displacementEnabled)
        {
            // Backwards compat: mesh
            return ResolveTelegraphMeshMaterialFor(displacementEnabled);
        }

        private Material ResolveTelegraphLineMaterial()
        {
            if (_telegraphProvider != null)
                return _telegraphProvider.GetLineMaterial(_telegraphDisplacementEnabled, _effects);
            if (Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphMaterialService.Provider != null)
                return Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphMaterialService.Provider
                    .GetLineMaterial(_telegraphDisplacementEnabled, _effects);
            return new Material(Shader.Find("Sprites/Default"));
        }

        private Material ResolveTelegraphLineMaterialFor(bool displacementEnabled)
        {
            if (_telegraphProvider != null)
                return _telegraphProvider.GetLineMaterial(displacementEnabled, _effects);
            if (Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphMaterialService.Provider != null)
                return Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphMaterialService.Provider
                    .GetLineMaterial(displacementEnabled, _effects);
            return new Material(Shader.Find("Sprites/Default"));
        }

        private Material ResolveTelegraphMeshMaterial()
        {
            if (_telegraphProvider != null)
                return _telegraphProvider.GetMeshMaterial(_telegraphDisplacementEnabled, _effects);
            if (Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphMaterialService.Provider != null)
                return Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphMaterialService.Provider
                    .GetMeshMaterial(_telegraphDisplacementEnabled, _effects);
            return new Material(Shader.Find("Sprites/Default"));
        }

        private Material ResolveTelegraphMeshMaterialFor(bool displacementEnabled)
        {
            if (_telegraphProvider != null)
                return _telegraphProvider.GetMeshMaterial(displacementEnabled, _effects);
            if (Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphMaterialService.Provider != null)
                return Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphMaterialService.Provider
                    .GetMeshMaterial(displacementEnabled, _effects);
            return new Material(Shader.Find("Sprites/Default"));
        }

        public void TrySetTelegraphVisible(bool visible)
        {
            if (_handler is Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility tv)
            {
                tv.SetTelegraphVisible(visible);
            }
        }

        private CombatAttackVisualCatalogSO GetCombatAttackVisualCatalog()
        {
            if (_attackVisualCatalog != null) return _attackVisualCatalog;
            if (_resolvedCombatAttackVisualCatalog != null) return _resolvedCombatAttackVisualCatalog;
            try
            {
                var sceneCtxs = Object.FindObjectsByType<Zenject.SceneContext>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneCtxs.Length; i++)
                {
                    var sc = sceneCtxs[i];
                    if (sc == null || sc.gameObject.scene != gameObject.scene) continue;
                    try
                    {
                        _resolvedCombatAttackVisualCatalog = sc.Container.Resolve<CombatAttackVisualCatalogSO>();
                        return _resolvedCombatAttackVisualCatalog;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        /// <summary>Prefab mesh at scale 1 fills arena; small multi-cone = 0.5, large single cone = 1.</summary>
        private float ResolveConeTelegraphUniformScale()
        {
            if (_attackType == AttackType.WingSlash
                || _hokariAttackVisualId == HokariBossAttackVisualId.BigWindSlash
                || _hokariAttackVisualId == HokariBossAttackVisualId.BigCones)
                return 1f;
            return 0.5f;
        }

        /// <summary>Telegraph prefab from catalog row Orb or BigOrb (Pull/Push from effects when telegraph displacement is enabled). Used as <see cref="OrbView"/> AoE mesh; if null, disc stays procedural.</summary>
        private GameObject ResolveOrbAreaVisualPrefabFromCatalog()
        {
            var catalog = GetCombatAttackVisualCatalog();
            if (catalog == null) return null;
            bool hasGrapple = false;
            bool hasKnock = false;
            if (_telegraphDisplacementEnabled && _effects != null)
            {
                for (int i = 0; i < _effects.Count; i++)
                {
                    var fx = _effects[i];
                    if (fx == null) continue;
                    if (fx is Logic.Scripts.GameDomain.Effects.GrappleEffect) hasGrapple = true;
                    else if (fx is Logic.Scripts.GameDomain.Effects.KnockbackEffect) hasKnock = true;
                }
            }
            var row = _hokariAttackVisualId == HokariBossAttackVisualId.BigOrb
                ? HokariBossAttackVisualId.BigOrb
                : HokariBossAttackVisualId.Orb;
            return catalog.GetTelegraph(row, hasGrapple, hasKnock);
        }

        private HokariBossAttackVisualId ResolveCatalogVisualId()
        {
            if (_hokariAttackVisualId != HokariBossAttackVisualId.None)
                return _hokariAttackVisualId;
            return _attackType switch
            {
                AttackType.ProteanCones => HokariBossAttackVisualId.ProteanCones,
                AttackType.WingSlash => HokariBossAttackVisualId.WingSlash,
                AttackType.SkySwords => HokariBossAttackVisualId.SkySwords,
                AttackType.Circle => HokariBossAttackVisualId.Circle,
                AttackType.FeatherLines => HokariBossAttackVisualId.XZFeatherG,
                _ => HokariBossAttackVisualId.None,
            };
        }

        private GameObject ResolveVisualPrefabForDisplacement(bool displacementEnabled)
        {
            var catalog = GetCombatAttackVisualCatalog();
            if (catalog == null) return null;
            bool hasGrapple = false;
            bool hasKnock = false;
            if (displacementEnabled && _effects != null)
            {
                for (int i = 0; i < _effects.Count; i++)
                {
                    var fx = _effects[i];
                    if (fx == null) continue;
                    if (fx is Logic.Scripts.GameDomain.Effects.GrappleEffect) hasGrapple = true;
                    else if (fx is Logic.Scripts.GameDomain.Effects.KnockbackEffect) hasKnock = true;
                }
            }

            var vid = ResolveCatalogVisualId();
            if (vid == HokariBossAttackVisualId.None) return null;
            return catalog.GetTelegraph(vid, hasGrapple, hasKnock);
        }

        /// <summary>Gameplay hit areas for debug gizmos (Scene view / Gizmos).</summary>
        public void CollectDebugGizmoShapes(System.Collections.Generic.List<BossAttackDebugShape> sink, int turnsRemaining)
        {
            if (sink == null) return;

            Color color = turnsRemaining <= 0
                ? new Color(1f, 0.25f, 0.15f, 0.95f)
                : new Color(1f, 0.85f, 0.15f, 0.9f);
            string label = $"{GetAttackTypeName()} (T-{turnsRemaining})";

            Transform bossTr = GetComponentInParent<BossView>()?.transform ?? transform;
            Vector3 bossOrigin = bossTr.position;
            Vector3 bossForward = bossTr.forward;

            switch (_attackType)
            {
                case AttackType.ProteanCones:
                    AddConeFan(sink, bossOrigin, bossForward, _protean.radius, _protean.angleDeg, _protean.sides,
                        new float[] { 0f, 90f, 180f, 270f }, color, label);
                    break;
                case AttackType.WingSlash:
                {
                    float yaw = ResolveWingSlashYawDeg(bossTr);
                    AddConeFan(sink, bossOrigin, bossForward, _wingSlash.radius, Mathf.Abs(_wingSlash.angleDeg),
                        _wingSlash.sides, new float[] { yaw }, color, label);
                    break;
                }
                case AttackType.Circle:
                    sink.Add(new BossAttackDebugShape
                    {
                        Kind = BossAttackDebugShapeKind.Disc,
                        Origin = bossOrigin,
                        Radius = _circle.radius,
                        Color = color,
                        Label = label,
                    });
                    break;
                case AttackType.SkySwords:
                {
                    Vector3 center = ResolvePlayerWorldForDebug();
                    sink.Add(new BossAttackDebugShape
                    {
                        Kind = BossAttackDebugShapeKind.Disc,
                        Origin = center,
                        Radius = _skySwords.radius,
                        Color = color,
                        Label = label,
                    });
                    break;
                }
                case AttackType.Orb:
                    sink.Add(new BossAttackDebugShape
                    {
                        Kind = BossAttackDebugShapeKind.Disc,
                        Origin = bossOrigin,
                        Radius = _orb.initialRadius,
                        Color = color,
                        Label = label,
                    });
                    break;
                case AttackType.FeatherLines:
                    CollectFeatherStripShapes(sink, color, label);
                    break;
            }
        }

        void AddConeFan(
            System.Collections.Generic.List<BossAttackDebugShape> sink,
            Vector3 origin,
            Vector3 baseForward,
            float radius,
            float angleDeg,
            int sides,
            float[] yaws,
            Color color,
            string label)
        {
            if (yaws == null || yaws.Length == 0) return;
            Vector3 planarBase = Vector3.ProjectOnPlane(baseForward, Vector3.up);
            if (planarBase.sqrMagnitude < 1e-8f) planarBase = Vector3.forward;

            for (int i = 0; i < yaws.Length; i++)
            {
                Vector3 forward = Quaternion.Euler(0f, yaws[i], 0f) * planarBase;
                sink.Add(new BossAttackDebugShape
                {
                    Kind = BossAttackDebugShapeKind.Cone,
                    Origin = origin,
                    Forward = forward,
                    Radius = radius,
                    AngleDeg = angleDeg,
                    ConeSides = sides,
                    Color = color,
                    Label = label,
                });
            }
        }

        float ResolveWingSlashYawDeg(Transform bossTr)
        {
            float yawBase = -90f;
            try
            {
                Vector3 player = ResolvePlayerWorldForDebug();
                Vector3 toPlayer = player - bossTr.position;
                toPlayer.y = 0f;
                Vector3 fwd = bossTr.forward;
                fwd.y = 0f;
                if (toPlayer.sqrMagnitude > 1e-6f && fwd.sqrMagnitude > 1e-6f)
                {
                    toPlayer.Normalize();
                    fwd.Normalize();
                    float crossY = Vector3.Cross(fwd, toPlayer).y;
                    yawBase = crossY >= 0f ? -90f : 90f;
                }
            }
            catch { }

            return yawBase;
        }

        Vector3 ResolvePlayerWorldForDebug()
        {
            if (_arena != null)
                return _arena.RelativeArenaPositionToRealPosition(_arena.GetPlayerArenaPosition());

            var naraView = Object.FindFirstObjectByType<Logic.Scripts.GameDomain.MVC.Nara.NaraView>(FindObjectsInactive.Exclude);
            return naraView != null ? naraView.transform.position : transform.position;
        }

        void CollectFeatherStripShapes(System.Collections.Generic.List<BossAttackDebugShape> sink, Color color, string label)
        {
            Vector3 center = _arena != null ? _arena.transform.position : transform.position;
            var p = _feather;
            if (p.stripHalfExtent <= 0f)
                p.stripHalfExtent = _hokariArenaTelegraphHalfExtent;

            int n = Mathf.Max(1, p.featherCount);
            float halfWidth = Mathf.Max(0.05f, p.width * 0.5f);
            float h = Mathf.Max(0.1f, p.stripHalfExtent > 1e-4f ? p.stripHalfExtent : 10f);
            float spreadH = h * 1.4f;

            for (int i = 0; i < n; i++)
            {
                GetFeatherStripEndpoints(i, n, center, p.axisMode, h, spreadH, out Vector3 start, out Vector3 end);
                sink.Add(new BossAttackDebugShape
                {
                    Kind = BossAttackDebugShapeKind.Strip,
                    Origin = start,
                    StripEnd = end,
                    StripHalfWidth = halfWidth,
                    Color = color,
                    Label = label,
                });
            }
        }

        static void GetFeatherStripEndpoints(
            int index,
            int count,
            Vector3 center,
            FeatherAxisMode axisMode,
            float halfExtent,
            float spreadHalfExtent,
            out Vector3 start,
            out Vector3 end)
        {
            float y = center.y;
            switch (axisMode)
            {
                case FeatherAxisMode.X:
                {
                    float xCenter = center.x;
                    float z = center.z + FeatherStripSpreadCoordinate(index, count, spreadHalfExtent);
                    start = new Vector3(xCenter - halfExtent, y, z);
                    end = new Vector3(xCenter + halfExtent, y, z);
                    break;
                }
                case FeatherAxisMode.Z:
                {
                    float zCenter = center.z;
                    float x = center.x + FeatherStripSpreadCoordinate(index, count, spreadHalfExtent);
                    start = new Vector3(x, y, zCenter - halfExtent);
                    end = new Vector3(x, y, zCenter + halfExtent);
                    break;
                }
                case FeatherAxisMode.XZ:
                {
                    int nAlongX = (count + 1) / 2;
                    int nAlongZ = count / 2;
                    if ((index % 2) == 0)
                    {
                        int k = index / 2;
                        float xCenter = center.x;
                        float z = center.z + FeatherStripSpreadCoordinate(k, nAlongX, spreadHalfExtent);
                        start = new Vector3(xCenter - halfExtent, y, z);
                        end = new Vector3(xCenter + halfExtent, y, z);
                    }
                    else
                    {
                        int k = (index - 1) / 2;
                        float zCenter = center.z;
                        float x = center.x + FeatherStripSpreadCoordinate(k, nAlongZ, spreadHalfExtent);
                        start = new Vector3(x, y, zCenter - halfExtent);
                        end = new Vector3(x, y, zCenter + halfExtent);
                    }
                    break;
                }
                default:
                {
                    Vector3 d = new Vector3(1f, 0f, 1f).normalized;
                    float along = FeatherStripSpreadCoordinate(index, count, spreadHalfExtent);
                    Vector3 mid = new Vector3(center.x, y, center.z) + d * along;
                    start = mid - d * halfExtent;
                    end = mid + d * halfExtent;
                    break;
                }
            }
        }

        static float FeatherStripSpreadCoordinate(int index, int count, float halfExtent)
        {
            if (count <= 1) return 0f;
            float t = index / (float)(count - 1);
            return Mathf.Lerp(-halfExtent, halfExtent, t);
        }
    }
}
