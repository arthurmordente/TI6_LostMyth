using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadCanvasView : UguiCanvasViewBase, ILoadScreenView
{
    [SerializeField] private Button _closeButton;

    public void InitEntryPoint() => Hide();

    public void RegisterCallbacks(Action onGuide, Action onCheats, Action onCredits, Action onExit, Action onOptions)
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
    }
}
