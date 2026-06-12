using System.Collections.Generic;
using System.Linq;
using Logic.Scripts.GameDomain.Exploration;
using Logic.Scripts.GameDomain.MVC.ExplorationLoadout;
using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.Services.AudioService;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

public class ExplorationLoadoutUIController : IExplorationLoadoutUIController
{
    private readonly IExplorationLoadoutView _view;
    private readonly INewSkillSystemSkillLoadoutService _loadoutService;
    private readonly ISkillVisualCatalog _visualCatalog;
    private readonly IAudioService _audioService;

    private SkillDataSO _selectedCatalogSkill;
    private SkillDataSO _draggingSkill;
    private ExplorationLoadoutSkillFilter _catalogFilter = ExplorationLoadoutSkillFilter.All;
    private ExplorationLoadoutDivinityFilter _divinityFilter = ExplorationLoadoutDivinityFilter.All;
    private bool _modalGateActive;

    public bool IsVisible => _view != null && _view.IsVisible;

    public ExplorationLoadoutUIController(
        IExplorationLoadoutView view,
        INewSkillSystemSkillLoadoutService loadoutService,
        ISkillVisualCatalog visualCatalog,
        [InjectOptional] IAudioService audioService = null)
    {
        _view = view;
        _loadoutService = loadoutService;
        _visualCatalog = visualCatalog;
        _audioService = audioService;
    }

    public void InitEntryPoint()
    {
        if (_view == null) return;
        _view.Init(_visualCatalog);
        _view.RegisterCallbacks(Hide, OnCatalogFilterChanged, OnDivinityFilterChanged);
        _view.RegisterDragCallbacks(
            OnCatalogSkillPreview,
            OnCatalogDragBegin,
            OnCatalogDragMove,
            OnCatalogDragEnd,
            OnSkillDroppedOnSlot,
            OnEquippedSlotClicked);
        RebuildCatalog();
        RefreshSlots();
        if (_loadoutService != null)
            _loadoutService.OnLoadoutChanged += OnLoadoutChanged;
    }

    public void Toggle()
    {
        if (_view == null) return;
        if (_view.IsVisible) Hide();
        else Show();
    }

    public void Show()
    {
        if (_view == null || _view.IsVisible) return;
        GeneralSfxFeedback.PlayNpcTalking(_audioService);
        ExplorationModalInputGate.Push();
        ExplorationInteractInputGate.Push();
        _modalGateActive = true;
        RefreshSlots();
        RebuildCatalog();
        _view.SetVisible(true);
    }

    public void Hide()
    {
        if (_view == null || !_view.IsVisible) return;
        _view.HideDragGhost();
        _view.ClearAllSlotDropHighlights();
        LoadoutDragContext.Clear();
        _view.SetVisible(false);
        _draggingSkill = null;
        _selectedCatalogSkill = null;
        if (_modalGateActive)
        {
            ExplorationInteractInputGate.Pop();
            ExplorationModalInputGate.Pop();
            _modalGateActive = false;
        }
    }

    private void OnCatalogFilterChanged(ExplorationLoadoutSkillFilter filter)
    {
        _catalogFilter = filter;
        RebuildCatalog();
    }

    private void OnDivinityFilterChanged(ExplorationLoadoutDivinityFilter filter)
    {
        _divinityFilter = filter;
        RebuildCatalog();
    }

    private void RebuildCatalog()
    {
        if (_view == null || _loadoutService == null) return;
        _view.ClearCatalog();
        foreach (SkillDataSO skill in EnumerateFilteredCatalogSkills())
        {
            _view.CreateCatalogItem(skill, OnCatalogSkillClicked);
        }
        _view.FinalizeCatalogScroll();
        _view.SetSelectedCatalogSkill(_selectedCatalogSkill);
    }

    private IEnumerable<SkillDataSO> EnumerateFilteredCatalogSkills()
    {
        return _loadoutService.AllSkills
            .Where(s => s != null
                && ExplorationLoadoutSkillFilterUtil.Matches(s, _catalogFilter)
                && ExplorationLoadoutDivinityFilterUtil.Matches(s, _divinityFilter))
            .OrderBy(s => SkillDivinityUtil.CatalogSortOrder(s.Divinity))
            .ThenBy(s => s.SkillName ?? string.Empty, System.StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshSlots()
    {
        if (_view == null || _loadoutService == null) return;
        int slotCount = _loadoutService.SlotCount;
        _view.RebuildLoadoutSlots(slotCount, GetSkillForLoadoutSlot);
    }

    private SkillDataSO GetSkillForLoadoutSlot(SkillLoadoutUnitType unitType, int slotIndex)
    {
        if (_loadoutService == null) return null;
        _loadoutService.TryGetSelectedSkill(unitType, slotIndex, out SkillDataSO skill);
        return skill;
    }

    private void OnCatalogSkillPreview(SkillDataSO skill)
    {
        if (skill == null) return;
        _selectedCatalogSkill = skill;
        _view?.SetSelectedCatalogSkill(skill);
        _view?.ShowSkillDetails(skill);
    }

    private void OnCatalogSkillClicked(SkillDataSO skill)
    {
        if (skill == null) return;
        _selectedCatalogSkill = skill;
        _view?.SetSelectedCatalogSkill(skill);
        GeneralSfxFeedback.PlayMenuClick(_audioService);
        _view?.ShowSkillDetails(skill);
    }

    private void OnCatalogDragBegin(SkillDataSO skill)
    {
        if (skill == null) return;
        _draggingSkill = skill;
        _selectedCatalogSkill = skill;
        _view?.SetSelectedCatalogSkill(skill);
        _view?.ShowSkillDetails(skill);
        _view?.ShowDragGhost(skill);
        UpdateSlotDropHighlights(skill);
    }

    private void OnCatalogDragMove(Vector2 screenPosition)
    {
        _view?.UpdateDragGhostScreenPosition(screenPosition);
    }

    private void OnCatalogDragEnd()
    {
        _draggingSkill = null;
        _view?.HideDragGhost();
        _view?.ClearAllSlotDropHighlights();
    }

    private void UpdateSlotDropHighlights(SkillDataSO skill)
    {
        if (_view == null || _loadoutService == null || skill == null) return;

        int slotCount = _loadoutService.SlotCount;
        for (int i = 0; i < slotCount; i++)
        {
            bool canDropPlayer = _loadoutService.CanAssignSkillToSlot(SkillLoadoutUnitType.Player, i, skill);
            bool canDropBook = _loadoutService.CanAssignSkillToSlot(SkillLoadoutUnitType.Book, i, skill);
            _view.SetSlotDropHighlight(SkillLoadoutUnitType.Player, i, canDropPlayer);
            _view.SetSlotDropHighlight(SkillLoadoutUnitType.Book, i, canDropBook);
        }
    }

    private void OnSkillDroppedOnSlot(SkillLoadoutUnitType unitType, int slotIndex, SkillDataSO skill)
    {
        if (skill == null) return;

        _selectedCatalogSkill = skill;
        _view?.SetSelectedCatalogSkill(skill);

        if (_loadoutService == null)
        {
            OnCatalogDragEnd();
            return;
        }

        if (!_loadoutService.CanAssignSkillToSlot(unitType, slotIndex, skill))
        {
            PlayInvalidAssignFeedback(unitType, slotIndex);
            OnCatalogDragEnd();
            return;
        }

        if (_loadoutService.SetSlotSkill(unitType, slotIndex, skill))
        {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            _view?.ShowSkillDetails(skill);
        }

        OnCatalogDragEnd();
    }

    private void OnEquippedSlotClicked(SkillLoadoutUnitType unitType, int slotIndex)
    {
        if (_loadoutService == null) return;

        _selectedCatalogSkill = null;
        _view?.SetSelectedCatalogSkill(null);

        if (_loadoutService.TryGetSelectedSkill(unitType, slotIndex, out SkillDataSO skill))
            _view?.ShowSkillDetails(skill);
    }

    private void PlayInvalidAssignFeedback(SkillLoadoutUnitType unitType, int slotIndex)
    {
        _view?.PlayInvalidAssignFeedback(unitType, slotIndex);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnLoadoutChanged(SkillLoadoutUnitType unitType)
    {
        RefreshSlots();
    }
}
