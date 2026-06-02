using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coordinator for end-of-fight overlays. Assign defeat and victory prefab roots; only the matching screen is shown.
/// Buttons are resolved by name under each screen: <c>btn_Retry</c>, <c>btn_Return</c>, <c>btn_QuitGame</c>.
/// </summary>
public sealed class GameOverCanvasView : UguiCanvasViewBase, IGameOverView
{
    [Header("Screens")]
    [Tooltip("Root of Canvas_GameOver (derrota).")]
    [SerializeField] private GameObject _defeatScreen;
    [Tooltip("Root of Canvas_Victory (vitória).")]
    [SerializeField] private GameObject _victoryScreen;

    Button _defeatRetryButton;
    Button _defeatReturnButton;
    Button _defeatQuitButton;
    Button _victoryReturnButton;
    Button _victoryQuitButton;

    public void InitEntryPoint()
    {
        ResolveButtons();
        Hide();
    }

    void ResolveButtons()
    {
        if (_defeatScreen != null)
        {
            Transform root = _defeatScreen.transform;
            _defeatRetryButton = ResolveButton(_defeatRetryButton, root, "btn_Retry");
            _defeatReturnButton = ResolveButton(_defeatReturnButton, root, "btn_Return");
            _defeatQuitButton = ResolveButton(_defeatQuitButton, root, "btn_QuitGame");
        }

        if (_victoryScreen != null)
        {
            Transform root = _victoryScreen.transform;
            _victoryReturnButton = ResolveButton(_victoryReturnButton, root, "btn_Return");
            _victoryQuitButton = ResolveButton(_victoryQuitButton, root, "btn_QuitGame");
        }
    }

    public void RegisterCallbacks(Action onRetry, Action onReturnToLobby, Action onQuitGame)
    {
        WireButton(_defeatRetryButton, onRetry);
        WireButton(_defeatReturnButton, onReturnToLobby);
        WireButton(_defeatQuitButton, onQuitGame);
        WireButton(_victoryReturnButton, onReturnToLobby);
        WireButton(_victoryQuitButton, onQuitGame);
    }

    public void Show(bool isWin)
    {
        SetScreenActive(_defeatScreen, !isWin);
        SetScreenActive(_victoryScreen, isWin);
        transform.localScale = Vector3.one;
        base.Show();
    }

    public override void Hide()
    {
        SetScreenActive(_defeatScreen, false);
        SetScreenActive(_victoryScreen, false);
        base.Hide();
    }

    static void SetScreenActive(GameObject screen, bool active)
    {
        if (screen != null)
            screen.SetActive(active);
    }

    static void WireButton(Button button, Action callback)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => callback?.Invoke());
    }
}
