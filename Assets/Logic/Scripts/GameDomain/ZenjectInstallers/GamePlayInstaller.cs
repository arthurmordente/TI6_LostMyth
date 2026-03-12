using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.GameplayInitiator;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Zenject;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.GameDomain.MVC.Echo;
using Logic.Scripts.GameDomain.MVC.Boss.Telegraph;

public class GamePlayInstaller : MonoInstaller {

    [SerializeField] private NaraView _naraViewPrefab;
    [SerializeField] private NaraConfigurationSO _naraConfiguration;

    [SerializeField] private GamePlayUiView _gamePlayUiView;
    [SerializeField] private PauseUiView _pauseUiView;
    [SerializeField] private GameOverUIView _gameOverUIView;

    [SerializeField] private AbilityData[] _skills;

    [Header("Book Skills")]
    [Tooltip("Skills exclusivas do Livro. Se vazio, o Livro usará as mesmas skills da Nara.")]
    [SerializeField] private AbilityData[] _bookSkills;

    [SerializeField] private LayerMask _layerMaskMouse;
    [SerializeField] private EchoView _echoviewPrefab;

    [Header("Book System")]
    [SerializeField] private BookView _bookViewPrefab;
    [SerializeField] private NaraConfigurationSO _bookConfiguration;
    [Tooltip("AbilityData sem efeitos, apenas com TargetingStrategy (ex: PointTargeting). " +
             "Controla o cursor de posicionamento do Livro ao usar Dividir.")]
    [SerializeField] private AbilityData _divideTargetingData;

    [Header("Telegraph Materials")]
    [SerializeField] private TelegraphMaterialConfig _telegraphMaterials;

    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.Bind<IGamePlayInitiator>().To<GamePlayInitiator>().AsSingle().NonLazy();
        Container.BindInterfacesTo<LevelCancellationTokenService>().AsSingle().NonLazy();
        Container.Bind<INaraMovementControllerFactory>().To<NaraMovementControllerFactory>().AsSingle();
        Container.BindInterfacesTo<GamePlayDataService>().AsSingle().NonLazy();

        // Book system
        Container.Bind<IActiveUnitService>().To<ActiveUnitService>().AsSingle();
        Container.Bind<IDivideAbilityHandler>().To<DivideAbilityHandler>().AsSingle()
            .WithArguments(_divideTargetingData);

        if (_telegraphMaterials != null) {
            Debug.Log($"[GamePlayInstaller] Binding TelegraphMaterialConfig: {_telegraphMaterials.name}");
            Container.Bind<TelegraphMaterialConfig>().FromInstance(_telegraphMaterials).AsSingle();
            Container.BindInterfacesAndSelfTo<TelegraphMaterialProvider>().AsSingle();
            Container.BindInterfacesAndSelfTo<TelegraphLayeringService>().AsSingle();
            Container.BindInterfacesTo<TelegraphMaterialProviderBootstrap>().AsSingle().NonLazy();
        }
        else {
            Debug.LogWarning("[GamePlayInstaller] TelegraphMaterialConfig is NULL. Telegraphs will fallback to Sprites/Default.");
            Container.BindInterfacesTo<TelegraphMaterialProviderBootstrap>().AsSingle().NonLazy();
        }
    }

    private void BindControllers() {
        Container.BindInterfacesTo<GameInputActionsController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<GamePlayUiController>().AsSingle().WithArguments(_gamePlayUiView, _pauseUiView, _gameOverUIView).NonLazy();
        Container.BindInterfacesTo<LevelScenarioController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<NaraController>().AsSingle().WithArguments(_naraViewPrefab, _naraConfiguration, _skills).NonLazy();
        Container.BindInterfacesTo<CastController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<EchoController>().AsSingle().WithArguments(_echoviewPrefab).NonLazy();
        Container.BindInterfacesTo<PortalController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<InteractableObjectsController>().AsSingle().NonLazy();

        // Book controller — starts inactive (no view), activated by DivideAbilityHandler.
        // _bookConfiguration allows setting separate HP/AP/Movement values for the book.
        // Falls back to _naraConfiguration if _bookConfiguration is not assigned.
        var bookCfg = _bookConfiguration != null ? _bookConfiguration : _naraConfiguration;
        // If _bookSkills is empty/unassigned the Book mirrors Nara's ability set.
        var resolvedBookSkills = (_bookSkills != null && _bookSkills.Length > 0) ? _bookSkills : _skills;
        Container.BindInterfacesTo<BookController>().AsSingle()
            .WithArguments(_bookViewPrefab, bookCfg, resolvedBookSkills).NonLazy();
    }
}
