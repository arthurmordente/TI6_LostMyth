using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.UI;

public sealed class CheatsCanvasView : UguiCanvasViewBase, ICheatsScreenView
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private Toggle _lifeToggle;
    [SerializeField] private Toggle _actionPointsToggle;
    [SerializeField] private Toggle _movementToggle;

    public void InitEntryPoint() => Hide();

    public void RegisterCallbacks(Action onGuide, Action onLoad, Action onCredits, Action onExit, Action onOptions,
        Action<bool> onLifeToggle, Action<bool> onActionToggle, Action<bool> onMovementToggle)
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
        if (_lifeToggle != null) _lifeToggle.onValueChanged.AddListener(v => onLifeToggle?.Invoke(v));
        if (_actionPointsToggle != null) _actionPointsToggle.onValueChanged.AddListener(v => onActionToggle?.Invoke(v));
        if (_movementToggle != null) _movementToggle.onValueChanged.AddListener(v => onMovementToggle?.Invoke(v));
    }
}
