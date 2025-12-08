using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.GameplayInitiator;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Abilitys;
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

    [SerializeField] private LayerMask _layerMaskMouse;
    [SerializeField] private EchoView _echoviewPrefab;

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
        Container.BindInterfacesTo<NaraController>().AsSingle().WithArguments(_naraViewPrefab, _naraConfiguration).NonLazy();
        Container.BindInterfacesTo<CastController>().AsSingle().WithArguments(_skills).NonLazy();
        Container.BindInterfacesTo<EchoController>().AsSingle().WithArguments(_echoviewPrefab).NonLazy();
        Container.BindInterfacesTo<PortalController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<InteractableObjectsController>().AsSingle().NonLazy();
    }
}
