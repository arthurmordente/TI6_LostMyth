using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadCanvasView : UguiCanvasViewBase, ILoadScreenView
{
    public override bool IsVisible => base.IsVisible;

    [SerializeField] private Button _closeButton;

    protected override void Awake()
    {
        base.Awake();
        HideUntilOpened();
    }

    public void InitEntryPoint() => HideUntilOpened();

    public void RegisterCallbacks(Action onGuide, Action onCheats, Action onCredits, Action onExit, Action onOptions)
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
    }
}
