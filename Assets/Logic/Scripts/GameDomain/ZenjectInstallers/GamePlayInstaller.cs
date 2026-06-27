using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.GameplayInitiator;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Zenject;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.GameDomain.MVC.Echo;
using Logic.Scripts.GameDomain.MVC.Boss.Laki;
using Logic.Scripts.GameDomain.MVC.Boss.Telegraph;
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.GameDomain.Services.Skills.Debug;
using Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GamePlayInstaller : MonoInstaller {

    [SerializeField] private NaraView _naraViewPrefab;
    [SerializeField] private NaraConfigurationSO _naraConfiguration;

    [SerializeField] private GamePlayUiCanvasView _gamePlayHud;
    [SerializeField] private PauseMenuCanvasView _pauseMenuView;
    [SerializeField] private GameOverCanvasView _gameOverView;

    [SerializeField] private AbilityData[] _skills;

    [Header("Book Skills")]
    [Tooltip("Skills exclusivas do Livro. Se vazio, o Livro usará as mesmas skills da Nara.")]
    [SerializeField] private AbilityData[] _bookSkills;

    [SerializeField] private LayerMask _layerMaskMouse;
    [SerializeField] private EchoView _echoviewPrefab;

    [Header("Erzahler Animation")]
    [SerializeField] private ErzahlerAnimatorControllersSO _erzahlerAnimatorControllers;

    [Header("Book System")]
    [SerializeField] private BookView _bookViewPrefab;
    [SerializeField] private NaraConfigurationSO _bookConfiguration;
    [Tooltip("AbilityData sem efeitos, apenas com TargetingStrategy (ex: PointTargeting). " +
             "Controla o cursor de posicionamento do Livro ao usar Dividir.")]
    [SerializeField] private AbilityData _divideTargetingData;

    [Tooltip("VFX one-shot ao confirmar/cancelar o spawn do clone. O preview de mira fica em BookAOE → AoePrefab.")]
    [SerializeField] private DivideAbilityVfxConfigSO _divideAbilityVfxConfig;

    [Header("Telegraph Materials")]
    [SerializeField] private TelegraphMaterialConfig _telegraphMaterials;

    [Header("Boss combat visuals")]
    [Tooltip("Telegraphs / Laki tiles catalog for Hokari. If empty in builds, assign here; in Editor, defaults to CombatAttackVisualCatalog.asset under Boss/Visuals.")]
    [SerializeField] private CombatAttackVisualCatalogSO _combatAttackVisualCatalog;

    [Header("Combat feedback")]
    [Tooltip("Prefab with FloatingCombatNumberView + world TextMeshPro. If empty, numbers spawn with a runtime fallback.")]
    [SerializeField] private FloatingCombatNumberView _floatingCombatNumberPrefab;

    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.Bind<IGamePlayInitiator>().To<GamePlayInitiator>().AsSingle().NonLazy();
        Container.BindInterfacesTo<LevelCancellationTokenService>().AsSingle().NonLazy();
        Container.Bind<INaraMovementControllerFactory>().To<NaraMovementControllerFactory>().AsSingle();
        Container.BindInterfacesTo<GamePlayDataService>().AsSingle().NonLazy();
        Container.Bind<INewSkillSystemSkillTargetingPreviewService>().To<NewSkillSystemSkillTargetingPreviewService>().AsSingle();
        Container.Bind<NewSkillSystemDefaultSkillCastFlow>().AsSingle();
        Container.BindInterfacesTo<SkillCastBeneficiaryResolver>().AsSingle().NonLazy();

        Container.BindInterfacesTo<FloatingCombatNumberService>().AsSingle()
            .WithArguments(_floatingCombatNumberPrefab);
        Container.BindInterfacesTo<FloatingCombatNumberBootstrap>().AsSingle().NonLazy();
        Container.BindInterfacesTo<SkillAttackHitboxDebugService>().AsSingle().NonLazy();
        Container.BindInterfacesTo<SkillAttackHitboxDebugBootstrap>().AsSingle().NonLazy();

        // Book system
        Container.Bind<IActiveUnitService>().To<ActiveUnitService>().AsSingle();
        Container.Bind<IDivideAbilityHandler>().To<DivideAbilityHandler>().AsSingle()
            .WithArguments(_divideTargetingData, _divideAbilityVfxConfig);

        if (_telegraphMaterials != null) {
            // Debug.Log($"[GamePlayInstaller] Binding TelegraphMaterialConfig: {_telegraphMaterials.name}");
            Container.Bind<TelegraphMaterialConfig>().FromInstance(_telegraphMaterials).AsSingle();
            Container.BindInterfacesAndSelfTo<TelegraphMaterialProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<TelegraphLayeringService>().AsSingle();
            Container.BindInterfacesTo<TelegraphMaterialProviderBootstrap>().AsSingle().NonLazy();
        }
        else {
            Debug.LogWarning("[GamePlayInstaller] TelegraphMaterialConfig is NULL. Telegraphs will fallback to Sprites/Default.");
            Container.BindInterfacesTo<TelegraphMaterialProviderBootstrap>().AsSingle().NonLazy();
        }

        var catalog = _combatAttackVisualCatalog;
#if UNITY_EDITOR
        if (catalog == null) {
            catalog = AssetDatabase.LoadAssetAtPath<CombatAttackVisualCatalogSO>(
                "Assets/Logic/Scripts/GameDomain/MVC/Boss/Visuals/CombatAttackVisualCatalog.asset");
        }
#endif
        if (catalog != null) {
            Container.Bind<CombatAttackVisualCatalogSO>().FromInstance(catalog).AsSingle();
            // Debug.Log($"[GamePlayInstaller] Bound CombatAttackVisualCatalogSO: {catalog.name}");
        }
        else {
            Debug.LogWarning("[GamePlayInstaller] CombatAttackVisualCatalogSO is NULL — boss telegraphs from catalog will not resolve (placeholders / procedural fallbacks). Assign _combatAttackVisualCatalog for builds.");
        }

        var erzControllers = _erzahlerAnimatorControllers;
#if UNITY_EDITOR
        if (erzControllers == null) {
            erzControllers = AssetDatabase.LoadAssetAtPath<ErzahlerAnimatorControllersSO>(
                "Assets/Logic/Scripts/GameDomain/MVC/Nara/Animation/ErzahlerAnimatorControllers.asset");
        }
#endif
        if (erzControllers != null)
            Container.Bind<ErzahlerAnimatorControllersSO>().FromInstance(erzControllers).AsSingle();
        else
            Debug.LogWarning("[GamePlayInstaller] ErzahlerAnimatorControllersSO is NULL — run TI6/Animation/Build Erzahler & Laki Animator Controllers in the Editor.");
    }

    private void BindControllers() {
        Container.BindInterfacesTo<GameInputActionsController>().AsSingle().NonLazy();
        Container.Bind<Logic.Scripts.GameDomain.MVC.Ui.IGamePlayHudView>().FromInstance(_gamePlayHud).AsSingle();
        Container.Bind<IPauseMenuView>().FromInstance(ResolvePauseMenuView()).AsSingle();
        Container.Bind<IGameOverView>().FromInstance(ResolveGameOverView()).AsSingle();

        Container.BindInterfacesTo<GamePlayUiController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<LevelScenarioController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<NaraController>().AsSingle().WithArguments(_naraViewPrefab, _naraConfiguration, _skills).NonLazy();
        Container.BindInterfacesTo<CastController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<EchoController>().AsSingle().WithArguments(_echoviewPrefab).NonLazy();
        Container.BindInterfacesTo<PortalController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<InteractableObjectsController>().AsSingle().NonLazy();

        // Book controller — starts inactive (no view), activated by DivideAbilityHandler.
        // _bookConfiguration still controls book AP/movement; HP is shared with Nara via INaraController.
        // Falls back to _naraConfiguration if _bookConfiguration is not assigned.
        var bookCfg = _bookConfiguration != null ? _bookConfiguration : _naraConfiguration;
        // If _bookSkills is empty/unassigned the Book mirrors Nara's ability set.
        var resolvedBookSkills = (_bookSkills != null && _bookSkills.Length > 0) ? _bookSkills : _skills;
        Container.BindInterfacesTo<BookController>().AsSingle()
            .WithArguments(_bookViewPrefab, bookCfg, resolvedBookSkills).NonLazy();

        Container.BindInterfacesTo<LakiDiceCameraBridge>().AsSingle().NonLazy();
    }

    private IPauseMenuView ResolvePauseMenuView() {
        if (_pauseMenuView != null)
            return _pauseMenuView;
        var existing = GetComponentInChildren<PauseMenuCanvasView>(true);
        if (existing != null)
            return existing;
        return CreateOverlayRoot<PauseMenuCanvasView>(nameof(PauseMenuCanvasView), sortingOrder: 100);
    }

    private IGameOverView ResolveGameOverView() {
        if (_gameOverView != null)
            return _gameOverView;
        var existing = GetComponentInChildren<GameOverCanvasView>(true);
        if (existing != null)
            return existing;
        return CreateOverlayRoot<GameOverCanvasView>(nameof(GameOverCanvasView), sortingOrder: 110);
    }

    private T CreateOverlayRoot<T>(string objectName, int sortingOrder) where T : Component {
        var root = new GameObject(objectName, typeof(RectTransform));
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();
        return root.AddComponent<T>();
    }
}
