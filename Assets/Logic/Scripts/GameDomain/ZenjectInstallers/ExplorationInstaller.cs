using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;
using Zenject;

public class ExplorationInstaller : MonoInstaller {
    [SerializeField] private NaraView _naraViewPrefab;
    [SerializeField] private NaraConfigurationSO _naraConfiguration;
    [SerializeField] private CustomizeUIView _customizeUiView;
    [SerializeField] private ExplorationLoadoutUIView _explorationLoadoutUiView;

    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.Bind<IExplorationInitiator>().To<ExplorationInitiator>().AsSingle().NonLazy();
        Container.BindInterfacesTo<LevelCancellationTokenService>().AsSingle().NonLazy();
        Container.Bind<INaraMovementControllerFactory>().To<NaraMovementControllerFactory>().AsSingle();
        Container.BindInterfacesTo<GamePlayDataService>().AsSingle().NonLazy();
    }

    private void BindControllers() {
        Container.BindInterfacesTo<GameInputActionsController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<LevelScenarioController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<NaraController>().AsSingle().WithArguments(_naraViewPrefab, _naraConfiguration).NonLazy();
        Container.BindInterfacesTo<PortalController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<InteractableObjectsController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<CustomizeUIController>().AsSingle().WithArguments(_customizeUiView).NonLazy();
        Container.Bind<ExplorationLoadoutUIView>().FromInstance(_explorationLoadoutUiView).AsSingle();
        Container.BindInterfacesTo<ExplorationLoadoutUIController>().AsSingle().NonLazy();
    }
}
