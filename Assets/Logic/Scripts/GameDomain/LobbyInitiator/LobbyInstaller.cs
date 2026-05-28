using UnityEngine;
using Zenject;

public class LobbyInstaller : MonoInstaller {
    [SerializeField] private LobbyMainMenuCanvasView _lobbyMenuView;

    public override void InstallBindings() {
        Container.Bind<ILobbyInitiator>().To<LobbyInitiator>().AsSingle().NonLazy();
        if (_lobbyMenuView != null)
            Container.Bind<ILobbyMenuView>().FromInstance(_lobbyMenuView).AsSingle();
        else
            Container.Bind<ILobbyMenuView>().FromComponentInHierarchy().AsSingle();
        Container.Bind<ILobbyController>().To<LobbyUiController>().AsSingle().NonLazy();
    }
}
