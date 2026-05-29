using UnityEngine;

public interface IUniversalUIController {
    Awaitable InitEntryPoint();
    /// <summary>Fecha o overlay modal no topo (opções, créditos, …). Usado por ESC e botões voltar.</summary>
    bool TryCloseTopOverlay();
    void CloseAllOverlays();
    void ShowLoadScreen();
    void ShowGuideScreen();
    void ShowCreditsScreen();
    void ShowOptionsScreen();
    void ShowCheatsScreen();
}
