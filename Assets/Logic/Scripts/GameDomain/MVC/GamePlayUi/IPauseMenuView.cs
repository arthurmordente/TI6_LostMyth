using System;

public interface IPauseMenuView
{
    void InitEntryPoint();
    void Show();
    void Hide();
    void RegisterCallbacks(Action onConfig, Action onCredits, Action onResume, Action onRetreat, Action onQuit);
}
