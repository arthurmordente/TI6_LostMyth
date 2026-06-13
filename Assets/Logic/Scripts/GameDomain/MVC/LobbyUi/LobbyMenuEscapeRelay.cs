using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

/// <summary>Lobby: ESC fecha overlays universais (opções, etc.).</summary>
public sealed class LobbyMenuEscapeRelay : MonoBehaviour
{
    IUniversalUIController _universalUIController;

    [Inject]
    void Construct([InjectOptional] IUniversalUIController universalUIController)
    {
        _universalUIController = universalUIController;
    }

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        var controller = ResolveUniversalUIController();
        if (controller == null) return;
        controller.TryCloseTopOverlay();
    }

    IUniversalUIController ResolveUniversalUIController()
    {
        if (_universalUIController != null) return _universalUIController;

        var sceneContexts = Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneContexts.Length; i++)
        {
            var sc = sceneContexts[i];
            if (sc == null) continue;
            try
            {
                _universalUIController = sc.Container.Resolve<IUniversalUIController>();
                if (_universalUIController != null) return _universalUIController;
            }
            catch { }
        }

        if (ProjectContext.Instance != null)
        {
            try
            {
                _universalUIController = ProjectContext.Instance.Container.Resolve<IUniversalUIController>();
            }
            catch { }
        }

        return _universalUIController;
    }
}
