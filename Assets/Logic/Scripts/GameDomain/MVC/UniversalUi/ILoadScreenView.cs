using System;

public interface ILoadScreenView
{
    void InitEntryPoint();
    void RegisterCallbacks(Action onGuide, Action onCheats, Action onCredits, Action onExit, Action onOptions);
    void Show();
    void Hide();
}
