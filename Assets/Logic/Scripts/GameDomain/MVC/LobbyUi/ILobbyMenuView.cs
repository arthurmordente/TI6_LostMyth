using System;

public interface ILobbyMenuView
{
    void Show();
    void Hide();
    void RegisterCallbacks(Action onPlay, Action onConfig, Action onQuit);
}
