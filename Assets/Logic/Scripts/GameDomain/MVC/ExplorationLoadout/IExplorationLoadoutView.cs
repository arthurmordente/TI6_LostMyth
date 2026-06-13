using System;
using Logic.Scripts.GameDomain.MVC.ExplorationLoadout;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

/// <summary>
/// View do menu de loadout de skills na ExplorationScene (canvas uGUI).
/// </summary>
public interface IExplorationLoadoutView
{
    bool IsVisible { get; }
    void Init(ISkillVisualCatalog visualCatalog = null);
    void SetVisible(bool visible);
    void RegisterCallbacks(Action onClose,
        Action<ExplorationLoadoutSkillFilter> onCatalogFilterChanged = null,
        Action<ExplorationLoadoutDivinityFilter> onDivinityFilterChanged = null,
        Action onClearCatalogSelection = null);
    void RegisterDragCallbacks(
        Action<SkillDataSO> onCatalogPreview,
        Action<SkillDataSO> onDragBegin,
        Action<Vector2> onDragMove,
        Action onDragEnd,
        Action<SkillLoadoutUnitType, int, SkillDataSO> onSkillDropped,
        Action<SkillLoadoutUnitType, int> onEquippedSlotClicked);
    void RebuildLoadoutSlots(int slotCount, Func<SkillLoadoutUnitType, int, SkillDataSO> getSkillForSlot);
    void PlayInvalidAssignFeedback(SkillLoadoutUnitType unitType, int slotIndex);
    void ClearCatalog();
    LoadoutSkillFrameView CreateCatalogItem(SkillDataSO skill, Action<SkillDataSO> onCatalogClicked);
    void FinalizeCatalogScroll();
    void ShowSkillDetails(SkillDataSO skill);
    void ShowDefaultDetailPanel();
    void SetSelectedCatalogSkill(SkillDataSO skill);
    void ShowDragGhost(SkillDataSO skill);
    void UpdateDragGhostScreenPosition(Vector2 screenPosition);
    void HideDragGhost();
    void SetSlotDropHighlight(SkillLoadoutUnitType unitType, int slotIndex, bool canDrop);
    void ClearAllSlotDropHighlights();
}
