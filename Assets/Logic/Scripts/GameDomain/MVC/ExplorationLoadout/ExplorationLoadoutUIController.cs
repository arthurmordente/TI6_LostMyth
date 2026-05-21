using System.Collections.Generic;
using System.Linq;
using Logic.Scripts.GameDomain.MVC.ExplorationLoadout;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine.EventSystems;

public class ExplorationLoadoutUIController : IExplorationLoadoutUIController
{
    private readonly ExplorationLoadoutUIView _view;
    private readonly INewSkillSystemSkillLoadoutService _loadoutService;

    private SkillLoadoutUnitType _selectedUnitType = SkillLoadoutUnitType.Player;
    private int _selectedSlotIndex;
    private ExplorationLoadoutSkillFilter _catalogFilter = ExplorationLoadoutSkillFilter.All;

    public ExplorationLoadoutUIController(ExplorationLoadoutUIView view, INewSkillSystemSkillLoadoutService loadoutService)
    {
        _view = view;
        _loadoutService = loadoutService;
    }

    public void InitEntryPoint()
    {
        if (_view == null) return;
        _view.Init();
        _view.RegisterCallbacks(Hide, OnPlayerSlotClicked, OnBookSlotClicked, OnCatalogFilterChanged);
        RebuildCatalog();
        RefreshSlots();
        _view.SetSelectedSlot(_selectedUnitType, _selectedSlotIndex);
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
        if (_view == null) return;
        RefreshSlots();
        RebuildCatalog();
        _view.SetVisible(true);
    }

    public void Hide()
    {
        if (_view == null) return;
        _view.SetVisible(false);
    }

    private void OnCatalogFilterChanged(ExplorationLoadoutSkillFilter filter)
    {
        _catalogFilter = filter;
        RebuildCatalog();
    }

    private void RebuildCatalog()
    {
        if (_view == null || _loadoutService == null) return;
        _view.ClearCatalog();
        foreach (SkillDataSO skill in EnumerateFilteredCatalogSkills())
        {
            var item = _view.CreateCatalogItem();
            if (item == null) continue;
            item.Bind(skill, OnCatalogSkillSelected, OnCatalogSkillHovered);
        }
        _view.FinalizeCatalogScroll();
    }

    private IEnumerable<SkillDataSO> EnumerateFilteredCatalogSkills()
    {
        IEnumerable<SkillDataSO> q = _loadoutService.AllSkills
            .Where(s => s != null && ExplorationLoadoutSkillFilterUtil.Matches(s, _catalogFilter));
        if (_catalogFilter == ExplorationLoadoutSkillFilter.All)
            return q
                .OrderBy(s => ExplorationLoadoutSkillFilterUtil.AllViewSortGroup(s.SkillType))
                .ThenBy(s => s.SkillName ?? string.Empty, System.StringComparer.OrdinalIgnoreCase);
        return q.OrderBy(s => s.SkillName ?? string.Empty, System.StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshSlots()
    {
        if (_view == null || _loadoutService == null) return;

        for (int i = 0; i < _loadoutService.SlotCount; i++)
        {
            _loadoutService.TryGetSelectedSkill(SkillLoadoutUnitType.Player, i, out SkillDataSO playerSkill);
            _view.SetSlotData(SkillLoadoutUnitType.Player, i, playerSkill);

            _loadoutService.TryGetSelectedSkill(SkillLoadoutUnitType.Book, i, out SkillDataSO bookSkill);
            _view.SetSlotData(SkillLoadoutUnitType.Book, i, bookSkill);
        }
    }

    private void OnCatalogSkillSelected(SkillDataSO skill)
    {
        if (_loadoutService == null || skill == null) return;

        if (!_loadoutService.CanAssignSkillToSlot(_selectedUnitType, _selectedSlotIndex, skill))
        {
            PlayInvalidAssignFeedback();
            return;
        }

        if (_loadoutService.SetSlotSkill(_selectedUnitType, _selectedSlotIndex, skill))
            _view?.ShowSkillDetails(skill);
    }

    private void PlayInvalidAssignFeedback()
    {
        _view?.PlayInvalidAssignFeedback();
        _view?.ClearSlotSelection();
        _view?.ShowSkillDetails(null);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnCatalogSkillHovered(SkillDataSO skill)
    {
        _view?.ShowSkillDetails(skill);
    }

    private void OnPlayerSlotClicked(int slotIndex)
    {
        _selectedUnitType = SkillLoadoutUnitType.Player;
        _selectedSlotIndex = slotIndex;
        _view?.SetSelectedSlot(_selectedUnitType, _selectedSlotIndex);
        if (_loadoutService != null && _loadoutService.TryGetSelectedSkill(_selectedUnitType, slotIndex, out SkillDataSO skill))
            _view?.ShowSkillDetails(skill);
    }

    private void OnBookSlotClicked(int slotIndex)
    {
        _selectedUnitType = SkillLoadoutUnitType.Book;
        _selectedSlotIndex = slotIndex;
        _view?.SetSelectedSlot(_selectedUnitType, _selectedSlotIndex);
        if (_loadoutService != null && _loadoutService.TryGetSelectedSkill(_selectedUnitType, slotIndex, out SkillDataSO skill))
            _view?.ShowSkillDetails(skill);
    }

    private void OnLoadoutChanged(SkillLoadoutUnitType unitType)
    {
        RefreshSlots();
    }
}
