using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.UI;

public sealed class PauseMenuCanvasView : UguiCanvasViewBase, IPauseMenuView
{
    [SerializeField] private Button _configButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _retreatButton;
    [SerializeField] private Button _returnButton;
    [SerializeField] private Button _quitButton;

    public void InitEntryPoint()
    {
        var root = transform;
        _configButton = ResolveButton(_configButton, root, "btn_Config");
        _creditsButton = ResolveButton(_creditsButton, root, "btn_Credits");
        _retreatButton = ResolveButton(_retreatButton, root, "btn_Retreat");
        _returnButton = ResolveButton(_returnButton, root, "btn_Return");
        _quitButton = ResolveButton(_quitButton, root, "btn_Quit");
        Hide();
    }

    public void RegisterCallbacks(Action onConfig, Action onCredits, Action onResume, Action onRetreat, Action onQuit)
    {
        if (_configButton != null) _configButton.onClick.AddListener(() => onConfig?.Invoke());
        if (_creditsButton != null) _creditsButton.onClick.AddListener(() => onCredits?.Invoke());
        if (_returnButton != null) _returnButton.onClick.AddListener(() => onResume?.Invoke());
        if (_retreatButton != null) _retreatButton.onClick.AddListener(() => onRetreat?.Invoke());
        if (_quitButton != null) _quitButton.onClick.AddListener(() => onQuit?.Invoke());
    }

    public override void Show()
    {
        transform.localScale = Vector3.one;
        base.Show();
    }
}
