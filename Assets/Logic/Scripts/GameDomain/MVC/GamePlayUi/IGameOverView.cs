using System;

public interface IGameOverView
{
    void InitEntryPoint();
    void RegisterCallbacks(Action onRetry, Action onReturnToLobby, Action onQuitGame);
    void Show(bool isWin);
    void Hide();
}
