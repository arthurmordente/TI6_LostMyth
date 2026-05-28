using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CustomizeCanvasView : UguiCanvasViewBase, ICustomizeUIView
{
    [SerializeField] private TMP_Text _balanceLabel;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _plotButton;
    [SerializeField] private Button _ability1Slot;
    [SerializeField] private Button _ability2Slot;
    [SerializeField] private Button _ability3Slot;
    [SerializeField] private Button _ability4Slot;
    [SerializeField] private Button _ability5Slot;
    [SerializeField] private Button _damagePlusButton;
    [SerializeField] private Button _damageMinusButton;
    [SerializeField] private Button _cooldownPlusButton;
    [SerializeField] private Button _cooldownMinusButton;
    [SerializeField] private Button _costPlusButton;
    [SerializeField] private Button _costMinusButton;
    [SerializeField] private Button _rangePlusButton;
    [SerializeField] private Button _rangeMinusButton;

    public void InitStartPoint(AbilityData data)
    {
        HideCustomize();
    }

    public void SetAbility(AbilityData data) { }

    public void ShowCustomize() => Show();

    public void HideCustomize() => Hide();

    public void RegisterCallbacks(Action onDamagePlus, Action onDamageMinus, Action onCooldownPlus, Action onCooldownMinus,
        Action onCostPlus, Action onCostMinus, Action onRangePlus, Action onRangeMinus,
        Action onSetAbility1, Action onSetAbility2, Action onSetAbility3, Action onSetAbility4, Action onSetAbility5,
        Action onClickExit, Action onPlotPressed, Action onApplyPressed)
    {
        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(HideCustomize);
            _exitButton.onClick.AddListener(() => onClickExit?.Invoke());
        }
        if (_applyButton != null) _applyButton.onClick.AddListener(() => onApplyPressed?.Invoke());
        if (_plotButton != null) _plotButton.onClick.AddListener(() => onPlotPressed?.Invoke());
        BindAbilitySlot(_ability1Slot, onSetAbility1);
        BindAbilitySlot(_ability2Slot, onSetAbility2);
        BindAbilitySlot(_ability3Slot, onSetAbility3);
        BindAbilitySlot(_ability4Slot, onSetAbility4);
        BindAbilitySlot(_ability5Slot, onSetAbility5);
        BindStat(_damagePlusButton, onDamagePlus);
        BindStat(_damageMinusButton, onDamageMinus);
        BindStat(_cooldownPlusButton, onCooldownPlus);
        BindStat(_cooldownMinusButton, onCooldownMinus);
        BindStat(_costPlusButton, onCostPlus);
        BindStat(_costMinusButton, onCostMinus);
        BindStat(_rangePlusButton, onRangePlus);
        BindStat(_rangeMinusButton, onRangeMinus);
    }

    private static void BindAbilitySlot(Button button, Action callback)
    {
        if (button != null) button.onClick.AddListener(() => callback?.Invoke());
    }

    private static void BindStat(Button button, Action callback)
    {
        if (button != null) button.onClick.AddListener(() => callback?.Invoke());
    }

    public void SetSignOnOff(AbilityStat type, bool isMinus, bool newState)
    {
        var button = ResolveStatButton(type, isMinus);
        if (button != null) button.interactable = newState;
    }

    public void SetAllMinusSign(bool newState)
    {
        SetButtonInteractable(_damageMinusButton, newState);
        SetButtonInteractable(_cooldownMinusButton, newState);
        SetButtonInteractable(_costMinusButton, newState);
        SetButtonInteractable(_rangeMinusButton, newState);
    }

    public void SetAllPlusSign(bool newState)
    {
        SetButtonInteractable(_damagePlusButton, newState);
        SetButtonInteractable(_cooldownPlusButton, newState);
        SetButtonInteractable(_costPlusButton, newState);
        SetButtonInteractable(_rangePlusButton, newState);
    }

    public void SetUpBalanceText(string balanceText)
    {
        if (_balanceLabel != null) _balanceLabel.text = balanceText;
    }

    private Button ResolveStatButton(AbilityStat type, bool isMinus) =>
        type switch
        {
            AbilityStat.Damage => isMinus ? _damageMinusButton : _damagePlusButton,
            AbilityStat.Cooldown => isMinus ? _cooldownMinusButton : _cooldownPlusButton,
            AbilityStat.Cost => isMinus ? _costMinusButton : _costPlusButton,
            AbilityStat.Range => isMinus ? _rangeMinusButton : _rangePlusButton,
            _ => null
        };

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }
}
