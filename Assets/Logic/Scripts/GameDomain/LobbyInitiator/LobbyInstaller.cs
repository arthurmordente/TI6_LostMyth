using UnityEngine;
using Zenject;

public class LobbyInstaller : MonoInstaller {
    [SerializeField] private LobbyMainMenuCanvasView _lobbyMenuView;

    public override void InstallBindings() {
        Container.Bind<ILobbyInitiator>().To<LobbyInitiator>().AsSingle().NonLazy();

        if (_lobbyMenuView != null)
            Container.Bind<ILobbyMenuView>().FromInstance(_lobbyMenuView).AsSingle();
        else
            Container.Bind<ILobbyMenuView>().FromComponentInHierarchy().AsSingle()
                .OnInstantiated<ILobbyMenuView>((_, view) => AttachEscapeRelay(view));

        AttachEscapeRelay(_lobbyMenuView);
        Container.Bind<ILobbyController>().To<LobbyUiController>().AsSingle().NonLazy();
    }

    static void AttachEscapeRelay(ILobbyMenuView view)
    {
        if (view is not Component component) return;
        if (component.GetComponent<LobbyMenuEscapeRelay>() == null)
            component.gameObject.AddComponent<LobbyMenuEscapeRelay>();
    }

    static void AttachEscapeRelay(LobbyMainMenuCanvasView view)
    {
        if (view == null) return;
        AttachEscapeRelay((ILobbyMenuView)view);
    }
}
