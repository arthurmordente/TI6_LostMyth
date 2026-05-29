using System;
using Logic.Scripts.GameDomain.MVC.ExplorationLoadout;
using Logic.Scripts.GameDomain.Services.Skills;

/// <summary>
/// View do menu de loadout de skills na ExplorationScene (canvas uGUI).
/// </summary>
public interface IExplorationLoadoutView
{
    bool IsVisible { get; }
    void Init(ISkillVisualCatalog visualCatalog = null);
    void SetVisible(bool visible);
    void RegisterCallbacks(Action onClose, Action<int> onPlayerSlotClicked, Action<int> onBookSlotClicked,
        Action<ExplorationLoadoutSkillFilter> onCatalogFilterChanged = null,
        Action<ExplorationLoadoutDivinityFilter> onDivinityFilterChanged = null);
    void SetSlotData(SkillLoadoutUnitType unitType, int slotIndex, SkillDataSO skill);
    void SetSelectedSlot(SkillLoadoutUnitType unitType, int slotIndex);
    void ClearSlotSelection();
    void PlayInvalidAssignFeedback();
    void ClearCatalog();
    ExplorationSkillCatalogItemView CreateCatalogItem();
    void FinalizeCatalogScroll();
    void ShowSkillDetails(SkillDataSO skill);
}
