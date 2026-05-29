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
        if (skill == null) return;

        _selectedCatalogSkill = skill;
        _view?.SetSelectedCatalogSkill(skill);
        _view?.ShowSkillDetails(skill);
        GeneralSfxFeedback.PlayMenuClick(_audioService);
    }

    private void TryAssignSelectedCatalogSkillToSlot()
    {
        if (_loadoutService == null || _selectedCatalogSkill == null) return;

        if (!_loadoutService.CanAssignSkillToSlot(_selectedUnitType, _selectedSlotIndex, _selectedCatalogSkill))
        {
            PlayInvalidAssignFeedback();
            return;
        }

        if (_loadoutService.SetSlotSkill(_selectedUnitType, _selectedSlotIndex, _selectedCatalogSkill))
        {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            _view?.ShowSkillDetails(_selectedCatalogSkill);
        }
    }

    private void PlayInvalidAssignFeedback()
    {
        _view?.PlayInvalidAssignFeedback();
        _view?.ClearSlotSelection();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnPlayerSlotClicked(int slotIndex)
    {
        _selectedUnitType = SkillLoadoutUnitType.Player;
        _selectedSlotIndex = slotIndex;
        _view?.SetSelectedSlot(_selectedUnitType, _selectedSlotIndex);

        if (_selectedCatalogSkill != null)
        {
            TryAssignSelectedCatalogSkillToSlot();
            return;
        }

        if (_loadoutService != null && _loadoutService.TryGetSelectedSkill(_selectedUnitType, slotIndex, out SkillDataSO skill))
            _view?.ShowSkillDetails(skill);
    }

    private void OnBookSlotClicked(int slotIndex)
    {
        _selectedUnitType = SkillLoadoutUnitType.Book;
        _selectedSlotIndex = slotIndex;
        _view?.SetSelectedSlot(_selectedUnitType, _selectedSlotIndex);

        if (_selectedCatalogSkill != null)
        {
            TryAssignSelectedCatalogSkillToSlot();
            return;
        }

        if (_loadoutService != null && _loadoutService.TryGetSelectedSkill(_selectedUnitType, slotIndex, out SkillDataSO skill))
            _view?.ShowSkillDetails(skill);
    }

    private void OnLoadoutChanged(SkillLoadoutUnitType unitType)
    {
        RefreshSlots();
    }
}
