using Logic.Scripts.GameDomain.Exploration.Pause;
using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExplorationInstaller : MonoInstaller {
    [SerializeField] private NaraView _naraViewPrefab;
    [SerializeField] private NaraConfigurationSO _naraConfiguration;

    [Header("UI — ExplorationScene")]
    [Tooltip("Canvas de loadout (NPC Oganjdan). Componente ExplorationLoadoutCanvasView no root do canvas.")]
    [SerializeField] private ExplorationLoadoutCanvasView _loadoutMenuView;

    [Tooltip("Canvas de pause. Componente PauseMenuCanvasView no root do canvas.")]
    [SerializeField] private PauseMenuCanvasView _pauseMenuView;

    [Tooltip("Canvas do painel de dicas do lobby. Componente LobbyTipsPanelCanvasView no root do canvas.")]
    [SerializeField] private LobbyTipsPanelCanvasView _lobbyTipsPanelView;

    [SerializeField] private ErzahlerAnimatorControllersSO _erzahlerAnimatorControllers;

    public override void InstallBindings() {
        BindServices();
        BindControllers();
    }

    private void BindServices() {
        Container.Bind<IExplorationInitiator>().To<ExplorationInitiator>().AsSingle().NonLazy();
        Container.BindInterfacesTo<LevelCancellationTokenService>().AsSingle().NonLazy();
        Container.Bind<INaraMovementControllerFactory>().To<NaraMovementControllerFactory>().AsSingle();
        Container.BindInterfacesTo<GamePlayDataService>().AsSingle().NonLazy();

        var erzControllers = _erzahlerAnimatorControllers;
#if UNITY_EDITOR
        if (erzControllers == null) {
            erzControllers = AssetDatabase.LoadAssetAtPath<ErzahlerAnimatorControllersSO>(
                "Assets/Logic/Scripts/GameDomain/MVC/Nara/Animation/ErzahlerAnimatorControllers.asset");
        }
#endif
        if (erzControllers != null)
            Container.Bind<ErzahlerAnimatorControllersSO>().FromInstance(erzControllers).AsSingle();
    }

    private void BindControllers() {
        Container.BindInterfacesTo<GameInputActionsController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<LevelScenarioController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<NaraController>().AsSingle().WithArguments(_naraViewPrefab, _naraConfiguration).NonLazy();
        Container.BindInterfacesTo<PortalController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<InteractableObjectsController>().AsSingle().NonLazy();
        Container.BindInterfacesTo<LobbyInteractionZoneController>().AsSingle().NonLazy();

        var loadoutView = ResolveLoadoutView();
        if (loadoutView == null) {
            Debug.LogError(
                "[ExplorationInstaller] Loadout menu view not found. Add ExplorationLoadoutCanvasView to your loadout canvas and assign it on this installer.",
                this);
        } else {
            Container.Bind<IExplorationLoadoutView>().FromInstance(loadoutView).AsSingle();
            Container.BindInterfacesTo<ExplorationLoadoutUIController>().AsSingle();
        }

        Container.Bind<IPauseMenuView>().FromInstance(ResolvePauseMenuView()).AsSingle();
        Container.BindInterfacesTo<ExplorationPauseController>().AsSingle();

        var lobbyTipsView = ResolveLobbyTipsView();
        if (lobbyTipsView != null) {
            Container.Bind<ILobbyTipsView>().FromInstance(lobbyTipsView).AsSingle();
            Container.BindInterfacesTo<LobbyTipsUIController>().AsSingle();
        } else {
            Debug.LogWarning(
                "[ExplorationInstaller] Lobby tips panel view not found. F tips interaction will be unavailable until LobbyTipsPanelCanvasView is assigned.",
                this);
        }
    }

    private IExplorationLoadoutView ResolveLoadoutView() {
        if (_loadoutMenuView != null)
            return _loadoutMenuView;

        var canvasView = GetComponentInChildren<ExplorationLoadoutCanvasView>(true);
        if (canvasView != null)
            return canvasView;

        var legacyView = GetComponentInChildren<ExplorationLoadoutUIView>(true);
        return legacyView;
    }

    private IPauseMenuView ResolvePauseMenuView() {
        if (_pauseMenuView != null)
            return _pauseMenuView;

        var existing = GetComponentInChildren<PauseMenuCanvasView>(true);
        if (existing != null)
            return existing;

        return CreateOverlayRoot<PauseMenuCanvasView>(nameof(PauseMenuCanvasView), sortingOrder: 100);
    }

    private ILobbyTipsView ResolveLobbyTipsView() {
        LobbyTipsPanelCanvasView view = null;

        if (_lobbyTipsPanelView != null)
            view = _lobbyTipsPanelView;
        else
            view = GetComponentInChildren<LobbyTipsPanelCanvasView>(true);

        if (view == null)
            return CreateOverlayRoot<LobbyTipsPanelCanvasView>(nameof(LobbyTipsPanelCanvasView), sortingOrder: 120);

        if (!view.gameObject.scene.IsValid())
            view = Instantiate(view, transform);

        return view;
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
