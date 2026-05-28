using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyMainMenuCanvasView : UguiCanvasViewBase, ILobbyMenuView
{
    [Header("Buttons (optional — resolved by name under root)")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _configButton;
    [SerializeField] private Button _quitButton;

    public void RegisterCallbacks(Action onPlay, Action onConfig, Action onQuit)
    {
        var root = transform;
        _startButton = ResolveButton(_startButton, root, "btn_StartGame");
        _configButton = ResolveButton(_configButton, root, "btn_Config");
        _quitButton = ResolveButton(_quitButton, root, "btn_QuitGame");

        if (_startButton != null) _startButton.onClick.AddListener(() => onPlay?.Invoke());
        if (_configButton != null) _configButton.onClick.AddListener(() => onConfig?.Invoke());
        if (_quitButton != null) _quitButton.onClick.AddListener(() => onQuit?.Invoke());
    }
}
