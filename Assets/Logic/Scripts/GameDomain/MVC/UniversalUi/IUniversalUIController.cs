using UnityEngine;

public interface IUniversalUIController {
    Awaitable InitEntryPoint();
    bool TryCloseTopOverlay();
    void CloseAllOverlays();
    void ShowLoadScreen();
    void ShowGuideScreen();
    void ShowCreditsScreen();
    void ShowOptionsScreen();
    void ShowCheatsScreen();
}
