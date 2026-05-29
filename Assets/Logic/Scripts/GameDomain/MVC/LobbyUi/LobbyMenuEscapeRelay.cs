using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

/// <summary>Lobby: ESC fecha overlays universais (opções, etc.).</summary>
public sealed class LobbyMenuEscapeRelay : MonoBehaviour
{
    private IUniversalUIController _universalUIController;

    [Inject]
    void Construct(IUniversalUIController universalUIController)
    {
        _universalUIController = universalUIController;
    }

    void Update()
    {
        if (_universalUIController == null) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        _universalUIController.TryCloseTopOverlay();
    }
}
