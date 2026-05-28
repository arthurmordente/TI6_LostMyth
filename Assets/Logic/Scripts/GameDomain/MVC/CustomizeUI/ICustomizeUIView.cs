using Logic.Scripts.GameDomain.MVC.Abilitys;
using System;

public interface ICustomizeUIView
{
    void InitStartPoint(AbilityData data);
    void SetAbility(AbilityData data);
    void ShowCustomize();
    void HideCustomize();
    void RegisterCallbacks(Action onDamagePlus, Action onDamageMinus, Action onCooldownPlus, Action onCooldownMinus,
        Action onCostPlus, Action onCostMinus, Action onRangePlus, Action onRangeMinus,
        Action onSetAbility1, Action onSetAbility2, Action onSetAbility3, Action onSetAbility4, Action onSetAbility5,
        Action onClickExit, Action onPlotPressed, Action onApplyPressed);
    void SetSignOnOff(AbilityStat type, bool isMinus, bool newState);
    void SetAllMinusSign(bool newState);
    void SetAllPlusSign(bool newState);
    void SetUpBalanceText(string balanceText);
}
