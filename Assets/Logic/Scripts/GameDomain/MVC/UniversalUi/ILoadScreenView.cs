using System;

public interface ILoadScreenView
{
    bool IsVisible { get; }
    void InitEntryPoint();
    void RegisterCallbacks(Action onGuide, Action onCheats, Action onCredits, Action onExit, Action onOptions);
    void Show();
    void Hide();
}
