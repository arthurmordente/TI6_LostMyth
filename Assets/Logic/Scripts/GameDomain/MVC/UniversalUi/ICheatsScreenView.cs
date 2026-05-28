using System;

public interface ICheatsScreenView
{
    bool IsVisible { get; }
    void InitEntryPoint();
    void RegisterCallbacks(Action onGuide, Action onLoad, Action onCredits, Action onExit, Action onOptions,
        Action<bool> onLifeToggle, Action<bool> onActionToggle, Action<bool> onMovementToggle);
    void Show();
    void Hide();
}
