using System.Collections.Generic;
using System.Linq;
using Logic.Scripts.GameDomain.Exploration;
using Logic.Scripts.GameDomain.MVC.ExplorationLoadout;
using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.Services.AudioService;
using UnityEngine.EventSystems;
using Zenject;

public class ExplorationLoadoutUIController : IExplorationLoadoutUIController
{
    private readonly IExplorationLoadoutView _view;
    private readonly INewSkillSystemSkillLoadoutService _loadoutService;
    private readonly ISkillVisualCatalog _visualCatalog;
    private readonly IAudioService _audioService;

    private SkillLoadoutUnitType _selectedUnitType = SkillLoadoutUnitType.Player;
    private int _selectedSlotIndex;
    private SkillDataSO _selectedCatalogSkill;
    private bool _slotArmedForAssign;
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
        _view.RegisterCallbacks(Hide, OnPlayerSlotClicked, OnBookSlotClicked,
            OnCatalogFilterChanged, OnDivinityFilterChanged);
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
        _view.SetVisible(false);
        _slotArmedForAssign = false;
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
            LoadoutSkillFrameView frame = _view.CreateCatalogItem();
            if (frame == null) continue;
            frame.Bind(skill, _visualCatalog, OnCatalogSkillSelected);
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
        if (_slotArmedForAssign)
            _view.SetSelectedSlot(_selectedUnitType, _selectedSlotIndex);
    }

    private SkillDataSO GetSkillForLoadoutSlot(SkillLoadoutUnitType unitType, int slotIndex)
    {
        if (_loadoutService == null) return null;
        _loadoutService.TryGetSelectedSkill(unitType, slotIndex, out SkillDataSO skill);
        return skill;
    }

    private void OnCatalogSkillSelected(SkillDataSO skill)
    {
        if (skill == null) return;

        _selectedCatalogSkill = skill;
        _view?.SetSelectedCatalogSkill(skill);

        if (_slotArmedForAssign)
        {
            TryAssignSelectedCatalogSkillToSlot();
            return;
        }

        _slotArmedForAssign = false;
        _view?.ClearSlotSelection();
        GeneralSfxFeedback.PlayMenuClick(_audioService);
        _view?.ShowSkillDetails(skill);
    }

    private bool TryAssignSelectedCatalogSkillToSlot()
    {
        if (_loadoutService == null || _selectedCatalogSkill == null) return false;

        if (!_loadoutService.CanAssignSkillToSlot(_selectedUnitType, _selectedSlotIndex, _selectedCatalogSkill))
        {
            PlayInvalidAssignFeedback();
            return false;
        }

        if (_loadoutService.SetSlotSkill(_selectedUnitType, _selectedSlotIndex, _selectedCatalogSkill))
        {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            _view?.ShowSkillDetails(_selectedCatalogSkill);
            return true;
        }

        return false;
    }

    private void PlayInvalidAssignFeedback()
    {
        _slotArmedForAssign = false;
        _view?.PlayInvalidAssignFeedback(_selectedUnitType, _selectedSlotIndex);
        _view?.ClearSlotSelection();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnPlayerSlotClicked(int slotIndex)
    {
        _selectedUnitType = SkillLoadoutUnitType.Player;
        _selectedSlotIndex = slotIndex;
        _slotArmedForAssign = true;
        _view?.SetSelectedSlot(_selectedUnitType, _selectedSlotIndex);

        if (_selectedCatalogSkill != null)
        {
            TryAssignSelectedCatalogSkillToSlot();
            return;
        }

        _selectedCatalogSkill = null;
        _view?.SetSelectedCatalogSkill(null);

        if (_loadoutService != null && _loadoutService.TryGetSelectedSkill(_selectedUnitType, slotIndex, out SkillDataSO skill))
            _view?.ShowSkillDetails(skill);
    }

    private void OnBookSlotClicked(int slotIndex)
    {
        _selectedUnitType = SkillLoadoutUnitType.Book;
        _selectedSlotIndex = slotIndex;
        _slotArmedForAssign = true;
        _view?.SetSelectedSlot(_selectedUnitType, _selectedSlotIndex);

        if (_selectedCatalogSkill != null)
        {
            TryAssignSelectedCatalogSkillToSlot();
            return;
        }

        _selectedCatalogSkill = null;
        _view?.SetSelectedCatalogSkill(null);

        if (_loadoutService != null && _loadoutService.TryGetSelectedSkill(_selectedUnitType, slotIndex, out SkillDataSO skill))
            _view?.ShowSkillDetails(skill);
    }

    private void OnLoadoutChanged(SkillLoadoutUnitType unitType)
    {
        RefreshSlots();
    }
}
