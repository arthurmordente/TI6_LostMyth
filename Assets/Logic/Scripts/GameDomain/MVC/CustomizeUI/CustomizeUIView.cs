using Logic.Scripts.GameDomain.MVC.Abilitys;
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class CustomizeUIView : MonoBehaviour {
    private Label _balanceLabel;

    private VisualElement _mainContainer;
    private VisualElement _skillContainer;
    private VisualElement _skillListContainer;
    private Button _customizeExitButton;
    private Button _applyButton;
    private Button _plotButton;
    private Button _ability1Slot;
    private Button _ability2Slot;
    private Button _ability3Slot;
    private Button _ability4Slot;
    private Button _ability5Slot;
    private Button _damagePlusButton;
    private Button _damageMinusButton;
    private Button _cooldownPlusButton;
    private Button _cooldownMinusButton;
    private Button _costPlusButton;
    private Button _costMinusButton;
    private Button _rangePlusButton;
    private Button _rangeMinusButton;

    public void InitStartPoint(AbilityData data) {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        _mainContainer = root.Q<VisualElement>("main-container");
        _customizeExitButton = root.Q<Button>("exit-customization-button");
        _plotButton = root.Q<Button>("plot-btn");
        _applyButton = root.Q<Button>("apply-btn");
        _skillContainer = root.Q<VisualElement>("skill-container");
        _skillListContainer = root.Q<VisualElement>("skill-list-container");
        _balanceLabel = root.Q<Label>("balance-txt");

        _ability1Slot = root.Q<Button>("ability-slot1-button");
        _ability2Slot = root.Q<Button>("ability-slot2-button");
        _ability3Slot = root.Q<Button>("ability-slot3-button");
        _ability4Slot = root.Q<Button>("ability-slot4-button");
        _ability5Slot = root.Q<Button>("ability-slot5-button");
        _damagePlusButton = root.Q<Button>("damage-plus-button");
        _damageMinusButton = root.Q<Button>("damage-minus-button");
        _cooldownPlusButton = root.Q<Button>("cooldown-plus-button");
        _cooldownMinusButton = root.Q<Button>("cooldown-minus-button");
        _costPlusButton = root.Q<Button>("costs-plus-button");
        _costMinusButton = root.Q<Button>("costs-minus-button");
        _rangePlusButton = root.Q<Button>("range-plus-button");
        _rangeMinusButton = root.Q<Button>("range-minus-button");
        SetAbility(data);
    }

    public void SetAbility(AbilityData data) {
        _skillContainer.dataSource = data;
        _skillListContainer.dataSource = data;
    }
    public void ShowCustomize() {
        _mainContainer.AddToClassList("open-container");
        _mainContainer.RemoveFromClassList("close-container");
    }

    public void HideCustomize() {
        _mainContainer.AddToClassList("close-container");
        _mainContainer.RemoveFromClassList("open-container");
    }

    public void RegisterCallbacks(Action OnDamagePlusPressed, Action OnDamageMinusPressed, Action OnCooldownPlusPressed,
        Action OnCooldownMinusPressed, Action OnCostPlusPressed, Action OnCostMinusPressed, Action OnRangePlusPressed,
        Action OnRangeMinusPressed, Action OnSetAbility1Pressed, Action OnSetAbility2Pressed, Action OnSetAbility3Pressed,
        Action OnSetAbility4Pressed, Action OnSetAbility5Pressed, Action OnClickExit, Action OnPlotPressed, Action OnApplyPressed) {
        _customizeExitButton.clicked += HideCustomize;
        _customizeExitButton.clicked += OnClickExit;
        _ability1Slot.clicked += OnSetAbility1Pressed;
        _ability2Slot.clicked += OnSetAbility2Pressed;
        _ability3Slot.clicked += OnSetAbility3Pressed;
        _ability4Slot.clicked += OnSetAbility4Pressed;
        _ability5Slot.clicked += OnSetAbility5Pressed;
        _damagePlusButton.clicked += OnDamagePlusPressed;
        _cooldownPlusButton.clicked += OnCooldownPlusPressed;
        _costPlusButton.clicked += OnCostPlusPressed;
        _rangePlusButton.clicked += OnRangePlusPressed;
        _damageMinusButton.clicked += OnDamageMinusPressed;
        _cooldownMinusButton.clicked += OnCooldownMinusPressed;
        _costMinusButton.clicked += OnCostMinusPressed;
        _rangeMinusButton.clicked += OnRangeMinusPressed;
        _plotButton.clicked += OnPlotPressed;
        _applyButton.clicked += OnApplyPressed;
    }

    #region UpdateButtonsAndText
    public void SetSignOnOff(AbilityStat type, bool isMinus, bool newState) {
        switch (type) {
            case AbilityStat.Damage:
                if (isMinus) _damageMinusButton.SetEnabled(newState);
                else _damagePlusButton.SetEnabled(newState);
                break;
            case AbilityStat.Cooldown:
                if (isMinus) _cooldownMinusButton.SetEnabled(newState);
                else _cooldownPlusButton.SetEnabled(newState);
                break;
            case AbilityStat.Cost:
                if (isMinus) _costMinusButton.SetEnabled(newState);
                else _costPlusButton.SetEnabled(newState);
                break;
            case AbilityStat.Range:
                if (isMinus) _rangeMinusButton.SetEnabled(newState);
                else _rangePlusButton.SetEnabled(newState);
                break;
        }
    }

    public void SetAllMinusSign(AbilityData data) {
        if (data.GetModifierStatValue(AbilityStat.Damage) > 1) _damageMinusButton.SetEnabled(true);
        else _damageMinusButton.SetEnabled(false);
        if (data.GetModifierStatValue(AbilityStat.Cooldown) > 1) _cooldownMinusButton.SetEnabled(true);
        else _cooldownMinusButton.SetEnabled(false);
        if (data.GetModifierStatValue(AbilityStat.Cost) > 1) _costMinusButton.SetEnabled(true);
        else _costMinusButton.SetEnabled(false);
        if (data.GetModifierStatValue(AbilityStat.Range) > 1) _rangeMinusButton.SetEnabled(true);
        else _rangeMinusButton.SetEnabled(false);
    }

    public void SetAllMinusSign(bool newState) {
        _damageMinusButton.SetEnabled(newState);
        _cooldownMinusButton.SetEnabled(newState);
        _costMinusButton.SetEnabled(newState);
        _rangeMinusButton.SetEnabled(newState);
    }

    public void SetAllPlusSign(bool newState) {
        _damagePlusButton.SetEnabled(newState);
        _cooldownPlusButton.SetEnabled(newState);
        _costPlusButton.SetEnabled(newState);
        _rangePlusButton.SetEnabled(newState);
    }

    public void SetUpBalanceText(string balanceText) {
        _balanceLabel.text = balanceText;
    }
    #endregion
}
