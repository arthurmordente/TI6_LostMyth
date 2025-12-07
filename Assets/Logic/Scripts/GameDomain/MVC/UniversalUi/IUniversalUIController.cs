using UnityEngine;

public interface IUniversalUIController {
    Awaitable InitEntryPoint();
    void ShowLoadScreen();
    void ShowGuideScreen();
    void ShowCreditsScreen();
    void ShowOptionsScreen();
    void ShowCheatsScreen();
}
