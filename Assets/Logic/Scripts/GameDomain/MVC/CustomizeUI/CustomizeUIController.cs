using Logic.Scripts.GameDomain.MVC.Abilitys;

public class CustomizeUIController : ICustomizeUIController {
    private readonly CustomizeUIView _customizationView;
    private readonly IAbilityPointService _abilityPointService;
    private AbilityData _selectedAbility;

    public CustomizeUIController(CustomizeUIView customizationView, IAbilityPointService abilityPointService) {
        _customizationView = customizationView;
        _abilityPointService = abilityPointService;
    }

    public void InitEntryPoint() {
        _selectedAbility = _abilityPointService.AllAbilities[0];
        _customizationView.InitStartPoint(_abilityPointService.AllAbilities[0]);
        HideCustomize();
        _customizationView.RegisterCallbacks(OnDamagePlus, OnDamageMinus, OnCooldownPlus, OnCooldownMinus, OnCostPlus,
            OnCostMinus, OnRangePlus, OnRangeMinus, OnAbility1Button, OnAbility2Button, OnAbility3Button, OnAbility4Button, OnAbility5Button);
        VerifyBalanceAndSetSigns();
    }

    public void ShowCustomize() {
        _customizationView.ShowCustomize();
    }

    public void HideCustomize() {
        _customizationView.HideCustomize();
    }


    public void VerifyBalanceAndSetSigns() {
        SetAllMinusSigns();
        SetAllPlusSigns();
        _customizationView.SetUpBalanceText(_abilityPointService.CurrentBalance.ToString("00") + "/" + _abilityPointService.AvailablePoints.ToString("00"));
    }

    private void SetAllMinusSigns() {
        if (_abilityPointService.CurrentBalance == 0) {
            _customizationView.SetAllMinusSign(false);
            return;
        }
        else {
            if (_selectedAbility.GetModifierStatValue(AbilityStat.Damage) > 0) _customizationView.SetSignOnOff(AbilityStat.Damage, true, true);
            else _customizationView.SetSignOnOff(AbilityStat.Damage, true, false);
            if (_selectedAbility.GetModifierStatValue(AbilityStat.Cooldown) > 0) _customizationView.SetSignOnOff(AbilityStat.Cooldown, true, true);
            else _customizationView.SetSignOnOff(AbilityStat.Cooldown, true, false);
            if (_selectedAbility.GetModifierStatValue(AbilityStat.Cost) > 0) _customizationView.SetSignOnOff(AbilityStat.Cost, true, true);
            else _customizationView.SetSignOnOff(AbilityStat.Cost, true, true);
            if (_selectedAbility.GetModifierStatValue(AbilityStat.Range) > 0) _customizationView.SetSignOnOff(AbilityStat.Range, true, true);
            else _customizationView.SetSignOnOff(AbilityStat.Range, true, true);
        }
    }

    private void SetAllPlusSigns() {
        if (_abilityPointService.CurrentBalance <= 0) {
            _customizationView.SetAllPlusSign(false);
        }
        else {
            _customizationView.SetAllPlusSign(true);
        }
    }

    #region SetAbilityButtonsCallbacks
    public void OnAbility1Button() {
        _selectedAbility = _abilityPointService.AllAbilities[0];
        _customizationView.SetAbility(_abilityPointService.AllAbilities[0]);
        VerifyBalanceAndSetSigns();
    }

    public void OnAbility2Button() {
        _selectedAbility = _abilityPointService.AllAbilities[1];
        _customizationView.SetAbility(_abilityPointService.AllAbilities[1]);
        VerifyBalanceAndSetSigns();
    }

    public void OnAbility3Button() {
        _selectedAbility = _abilityPointService.AllAbilities[2];
        _customizationView.SetAbility(_abilityPointService.AllAbilities[2]);
        VerifyBalanceAndSetSigns();
    }

    public void OnAbility4Button() {
        _selectedAbility = _abilityPointService.AllAbilities[3];
        _customizationView.SetAbility(_abilityPointService.AllAbilities[3]);
        VerifyBalanceAndSetSigns();
    }

    public void OnAbility5Button() {
        _selectedAbility = _abilityPointService.AllAbilities[4];
        _customizationView.SetAbility(_abilityPointService.AllAbilities[4]);
        VerifyBalanceAndSetSigns();
    }

    #endregion

    #region MinusPlusButtonsCallbacks
    public void OnDamagePlus() {
        if (_abilityPointService.TryIncreaseStat(_selectedAbility, AbilityStat.Damage)) {
            VerifyBalanceAndSetSigns();
        }
    }

    public void OnDamageMinus() {
        if (_abilityPointService.TryDecreaseStat(_selectedAbility, AbilityStat.Damage)) {
            VerifyBalanceAndSetSigns();
        }
    }

    public void OnCooldownPlus() {
        if (_abilityPointService.TryIncreaseStat(_selectedAbility, AbilityStat.Cooldown)) {
            VerifyBalanceAndSetSigns();
        }
    }

    public void OnCooldownMinus() {
        if (_abilityPointService.TryDecreaseStat(_selectedAbility, AbilityStat.Cooldown)) {
            VerifyBalanceAndSetSigns();
        }
    }

    public void OnCostPlus() {
        if (_abilityPointService.TryIncreaseStat(_selectedAbility, AbilityStat.Cost)) {
            VerifyBalanceAndSetSigns();
        }
    }

    public void OnCostMinus() {
        if (_abilityPointService.TryDecreaseStat(_selectedAbility, AbilityStat.Cost)) {
            VerifyBalanceAndSetSigns();
        }
    }

    public void OnRangePlus() {
        if (_abilityPointService.TryIncreaseStat(_selectedAbility, AbilityStat.Range)) {
            VerifyBalanceAndSetSigns();
        }
    }

    public void OnRangeMinus() {
        if (_abilityPointService.TryDecreaseStat(_selectedAbility, AbilityStat.Range)) {
            VerifyBalanceAndSetSigns();
        }
    }
    #endregion
}
