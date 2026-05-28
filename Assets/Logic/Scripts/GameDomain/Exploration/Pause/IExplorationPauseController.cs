public interface IExplorationPauseController
{
    bool IsVisible { get; }
    void InitEntryPoint();
    void ShowPauseScreen();
    void HidePauseScreen();
    void TogglePauseScreen();
}
