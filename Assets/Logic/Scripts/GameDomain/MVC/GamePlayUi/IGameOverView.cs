using System;

public interface IGameOverView
{
    void InitEntryPoint();
    void RegisterCallbacks(Action onPlay, Action onLoad, Action onExit);
    void Show(bool isWin);
    void Hide();
}
