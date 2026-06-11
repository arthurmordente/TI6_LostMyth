using System;
using UnityEngine;

public interface IGameOverView
{
    void InitEntryPoint();
    void RegisterCallbacks(Action onRetry, Action onReturnToLobby, Action onQuitGame);
    void Show(bool isWin);
    Awaitable ShowWithFadeAsync(bool isWin, float fadeDurationSeconds = 1f);
    void Hide();
}
