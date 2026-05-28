using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverCanvasView : UguiCanvasViewBase, IGameOverView
{
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _exitButton;

    public void InitEntryPoint() => Hide();

    public void RegisterCallbacks(Action onPlay, Action onLoad, Action onExit)
    {
        if (_playButton != null) _playButton.onClick.AddListener(() => onPlay?.Invoke());
        if (_loadButton != null) _loadButton.onClick.AddListener(() => onLoad?.Invoke());
        if (_exitButton != null) _exitButton.onClick.AddListener(() => onExit?.Invoke());
    }

    public void Show(bool isWin)
    {
        if (_resultText != null)
            _resultText.text = isWin ? "Você Ganhou" : "Derrotado";
        Show();
    }
}
