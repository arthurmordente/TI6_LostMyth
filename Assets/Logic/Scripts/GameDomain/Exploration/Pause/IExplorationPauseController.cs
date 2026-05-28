public interface IExplorationPauseController
{
    bool IsVisible { get; }
    void InitEntryPoint();
    void ShowPauseScreen();
    void HidePauseScreen();
    void TogglePauseScreen();
    void HandleEscapeWhilePaused();
    void ForceHide();
}
