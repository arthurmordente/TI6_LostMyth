using System.Collections.Generic;

using System.Linq;

using Logic.Scripts.GameDomain.Exploration;

using Logic.Scripts.GameDomain.MVC.ExplorationLoadout;

using Logic.Scripts.GameDomain.Services.Cheats;

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

    private readonly ILoadoutCheatService _cheatService;



    private SkillDataSO _selectedCatalogSkill;

    private CheatDataSO _selectedCheat;

    private SkillDataSO _draggingSkill;

    private ExplorationLoadoutSkillFilter _catalogFilter = ExplorationLoadoutSkillFilter.All;

    private ExplorationLoadoutDivinityFilter _divinityFilter = ExplorationLoadoutDivinityFilter.All;

    private LoadoutLeftCatalogTab _leftTab = LoadoutLeftCatalogTab.Skills;

    private LoadoutRightDetailTab _rightTab = LoadoutRightDetailTab.Overview;

    private bool _modalGateActive;



    public bool IsVisible => _view != null && _view.IsVisible;



    public ExplorationLoadoutUIController(

        IExplorationLoadoutView view,

        INewSkillSystemSkillLoadoutService loadoutService,

        ISkillVisualCatalog visualCatalog,

        [InjectOptional] IAudioService audioService = null,

        [InjectOptional] ILoadoutCheatService cheatService = null)

    {

        _view = view;

        _loadoutService = loadoutService;

        _visualCatalog = visualCatalog;

        _audioService = audioService;

        _cheatService = cheatService;

    }



    public void InitEntryPoint()

    {

        if (_view == null) return;

        _view.Init(_visualCatalog);

        _view.RegisterCallbacks(Hide, OnCatalogFilterChanged, OnDivinityFilterChanged, ClearCatalogSelection);

        _view.RegisterBookmarkCallbacks(

            OnSkillBookmarkClicked,

            OnCheatBookmarkClicked,

            OnDefaultBookmarkClicked);

        _view.RegisterCheatToggleCallback(OnCheatToggleChanged);

        _view.RegisterDragCallbacks(

            OnCatalogSkillPreview,

            OnCatalogDragBegin,

            OnCatalogDragMove,

            OnCatalogDragEnd,

            OnSkillDroppedOnSlot,

            OnEquippedSlotClicked);

        RebuildCatalog();

        RebuildCheatCatalog();

        RefreshSlots();

        _view.ResetToDefaultPanelLayout();

        if (_loadoutService != null)

            _loadoutService.OnLoadoutChanged += OnLoadoutChanged;

        if (_cheatService != null)

            _cheatService.OnCheatsChanged += OnCheatsChanged;

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

        RebuildCheatCatalog();

        ClearCatalogSelection();

        _view.ResetToDefaultPanelLayout();

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

        _selectedCheat = null;

        if (_modalGateActive)

        {

            ExplorationInteractInputGate.Pop();

            ExplorationModalInputGate.Pop();

            _modalGateActive = false;

        }

    }



    private void ClearCatalogSelection()

    {

        _selectedCatalogSkill = null;

        _selectedCheat = null;

        _view?.SetSelectedCatalogSkill(null);

        _view?.SetSelectedCheat(null);

        _rightTab = LoadoutRightDetailTab.Overview;

        _view?.SetRightDetailTab(_rightTab);

        SyncSlotDropHighlights();

    }



    private void OnSkillBookmarkClicked()

    {

        if (_leftTab == LoadoutLeftCatalogTab.Skills) return;

        _leftTab = LoadoutLeftCatalogTab.Skills;

        _selectedCheat = null;

        _view?.SetSelectedCheat(null);

        _view?.HideDragGhost();

        _view?.ClearAllSlotDropHighlights();

        _view?.SetLeftCatalogTab(_leftTab);

        _rightTab = LoadoutRightDetailTab.Overview;

        ApplyRightPanelForCurrentTabs();

        GeneralSfxFeedback.PlayMenuClick(_audioService);

    }



    private void OnCheatBookmarkClicked()

    {

        if (_leftTab == LoadoutLeftCatalogTab.Cheats) return;

        _leftTab = LoadoutLeftCatalogTab.Cheats;

        _selectedCatalogSkill = null;

        _view?.SetSelectedCatalogSkill(null);

        _view?.HideDragGhost();

        _view?.ClearAllSlotDropHighlights();

        _view?.SetLeftCatalogTab(_leftTab);

        _rightTab = LoadoutRightDetailTab.Overview;

        ApplyRightPanelForCurrentTabs();

        GeneralSfxFeedback.PlayMenuClick(_audioService);

    }



    private void OnDefaultBookmarkClicked()

    {

        if (_rightTab == LoadoutRightDetailTab.Overview) return;

        _rightTab = LoadoutRightDetailTab.Overview;

        _view?.SetRightDetailTab(_rightTab);

        GeneralSfxFeedback.PlayMenuClick(_audioService);

    }



    private void OnCheatToggleChanged(CheatDataSO cheat, bool enabled)

    {

        if (cheat == null || _cheatService == null) return;

        _cheatService.SetEnabled(cheat.CheatId, enabled);

        SyncCheatCatalogEnabledVisuals();

        GeneralSfxFeedback.PlayMenuClick(_audioService);

    }



    private void OnCheatsChanged()

    {

        SyncCheatCatalogEnabledVisuals();

        if (_selectedCheat == null) return;

        _view?.SetCheatToggleWithoutNotify(_cheatService != null && _cheatService.IsEnabled(_selectedCheat));

    }



    private void SyncCheatCatalogEnabledVisuals()

    {

        if (_view == null || _cheatService == null) return;

        _view.SyncCheatCatalogEnabledStates(cheat => cheat != null && _cheatService.IsEnabled(cheat));

    }



    private void SelectSkillAndShowDetail(SkillDataSO skill)

    {

        if (skill == null) return;

        _selectedCatalogSkill = skill;

        _selectedCheat = null;

        _view?.SetSelectedCatalogSkill(skill);

        _view?.SetSelectedCheat(null);

        _rightTab = LoadoutRightDetailTab.Detail;

        _view?.SetRightDetailTab(_rightTab);

        _view?.ShowSkillDetails(skill);

        SyncSlotDropHighlights();

    }



    private void SelectCheatAndShowDetail(CheatDataSO cheat)

    {

        if (cheat == null) return;

        _selectedCheat = cheat;

        _selectedCatalogSkill = null;

        _view?.SetSelectedCatalogSkill(null);

        _view?.SetSelectedCheat(cheat);

        _view?.ClearAllSlotDropHighlights();

        _rightTab = LoadoutRightDetailTab.Detail;

        _view?.SetRightDetailTab(_rightTab);

        bool enabled = _cheatService != null && _cheatService.IsEnabled(cheat);

        _view?.ShowCheatDetails(cheat, enabled);

    }



    private void ApplyRightPanelForCurrentTabs()

    {

        _view?.SetRightDetailTab(_rightTab);

        RefreshDetailPanelContent();

    }



    private void RefreshDetailPanelContent()

    {

        if (_rightTab != LoadoutRightDetailTab.Detail) return;



        if (_leftTab == LoadoutLeftCatalogTab.Skills && _selectedCatalogSkill != null)

            _view?.ShowSkillDetails(_selectedCatalogSkill);

        else if (_leftTab == LoadoutLeftCatalogTab.Cheats && _selectedCheat != null)

        {

            bool enabled = _cheatService != null && _cheatService.IsEnabled(_selectedCheat);

            _view?.ShowCheatDetails(_selectedCheat, enabled);

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



    private void RebuildCheatCatalog()

    {

        if (_view == null || _cheatService == null) return;

        _view.ClearCheatCatalog();

        foreach (CheatDataSO cheat in _cheatService.AllCheats)

        {

            if (cheat == null) continue;

            _view.CreateCheatCatalogItem(cheat, OnCheatClicked);

        }

        _view.FinalizeCheatCatalogScroll();

        _view.SetSelectedCheat(_selectedCheat);

        SyncCheatCatalogEnabledVisuals();

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

        SyncSlotDropHighlights();

    }



    private SkillDataSO GetSkillForLoadoutSlot(SkillLoadoutUnitType unitType, int slotIndex)

    {

        if (_loadoutService == null) return null;

        _loadoutService.TryGetSelectedSkill(unitType, slotIndex, out SkillDataSO skill);

        return skill;

    }



    private void OnCatalogSkillPreview(SkillDataSO skill) => SelectSkillAndShowDetail(skill);



    private void OnCatalogSkillClicked(SkillDataSO skill)

    {

        if (skill == null) return;

        SelectSkillAndShowDetail(skill);

        GeneralSfxFeedback.PlayMenuClick(_audioService);

    }



    private void OnCheatClicked(CheatDataSO cheat)

    {

        if (cheat == null) return;

        if (_leftTab != LoadoutLeftCatalogTab.Cheats)

        {

            _leftTab = LoadoutLeftCatalogTab.Cheats;

            _view?.SetLeftCatalogTab(_leftTab);

        }

        SelectCheatAndShowDetail(cheat);

        GeneralSfxFeedback.PlayMenuClick(_audioService);

    }



    private void OnCatalogDragBegin(SkillDataSO skill)

    {

        if (skill == null) return;

        _draggingSkill = skill;

        SelectSkillAndShowDetail(skill);

        _view?.ShowDragGhost(skill);

    }



    private void OnCatalogDragMove(Vector2 screenPosition)

    {

        _view?.UpdateDragGhostScreenPosition(screenPosition);

    }



    private void OnCatalogDragEnd()

    {

        _draggingSkill = null;

        _view?.HideDragGhost();

        SyncSlotDropHighlights();

    }



    private void SyncSlotDropHighlights()

    {

        if (_leftTab != LoadoutLeftCatalogTab.Skills || _selectedCatalogSkill == null)

        {

            _view?.ClearAllSlotDropHighlights();

            return;

        }



        UpdateSlotDropHighlights(_selectedCatalogSkill);

    }



    private void UpdateSlotDropHighlights(SkillDataSO skill)

    {

        if (_view == null || _loadoutService == null || skill == null) return;



        int slotCount = _loadoutService.SlotCount;

        for (int i = 0; i < slotCount; i++)

        {

            bool canDropPlayer = _loadoutService.CanDropSkillOnSlot(SkillLoadoutUnitType.Player, i, skill);

            bool canDropBook = _loadoutService.CanDropSkillOnSlot(SkillLoadoutUnitType.Book, i, skill);

            _view.SetSlotDropHighlight(SkillLoadoutUnitType.Player, i, canDropPlayer);

            _view.SetSlotDropHighlight(SkillLoadoutUnitType.Book, i, canDropBook);

        }

    }



    private void OnSkillDroppedOnSlot(SkillLoadoutUnitType unitType, int slotIndex, SkillDataSO skill)

    {

        if (skill == null) return;



        if (_loadoutService == null)

        {

            OnCatalogDragEnd();

            return;

        }



        if (!_loadoutService.CanDropSkillOnSlot(unitType, slotIndex, skill))

        {

            PlayInvalidAssignFeedback(unitType, slotIndex);

            OnCatalogDragEnd();

            return;

        }



        if (_loadoutService.TryAssignOrSwapSlotSkill(unitType, slotIndex, skill))

        {

            GeneralSfxFeedback.PlayMenuClick(_audioService);

            SelectSkillAndShowDetail(skill);

        }



        OnCatalogDragEnd();

    }



    private void OnEquippedSlotClicked(SkillLoadoutUnitType unitType, int slotIndex)

    {

        if (_loadoutService == null) return;



        if (!_loadoutService.TryGetSelectedSkill(unitType, slotIndex, out SkillDataSO skill) || skill == null)

        {

            ClearCatalogSelection();

            return;

        }



        if (_leftTab != LoadoutLeftCatalogTab.Skills)

        {

            _leftTab = LoadoutLeftCatalogTab.Skills;

            _view?.SetLeftCatalogTab(_leftTab);

        }



        SelectSkillAndShowDetail(skill);

        GeneralSfxFeedback.PlayMenuClick(_audioService);

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

