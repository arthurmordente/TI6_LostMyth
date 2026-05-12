using Logic.Scripts.GameDomain.Services.Skills;

public class ExplorationLoadoutUIController : IExplorationLoadoutUIController
{
    private readonly ExplorationLoadoutUIView _view;
    private readonly INewSkillSystemSkillLoadoutService _loadoutService;

    private SkillLoadoutUnitType _selectedUnitType = SkillLoadoutUnitType.Player;
    private int _selectedSlotIndex;

    public ExplorationLoadoutUIController(ExplorationLoadoutUIView view, INewSkillSystemSkillLoadoutService loadoutService)
    {
        _view = view;
        _loadoutService = loadoutService;
    }

    public void InitEntryPoint()
    {
        if (_view == null) return;
        _view.Init();
        _view.RegisterCallbacks(Hide, OnPlayerSlotClicked, OnBookSlotClicked);
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
        _view.SetVisible(true);
    }

    public void Hide()
    {
        if (_view == null) return;
        _view.SetVisible(false);
    }

    private void RebuildCatalog()
    {
        if (_view == null || _loadoutService == null) return;
        _view.ClearCatalog();
        foreach (SkillDataSO skill in _loadoutService.AllSkills)
        {
            var item = _view.CreateCatalogItem();
            if (item == null) continue;
            item.Bind(skill, OnCatalogSkillSelected, OnCatalogSkillHovered);
        }
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
        _loadoutService.SetSlotSkill(_selectedUnitType, _selectedSlotIndex, skill);
        _view?.ShowSkillDetails(skill);
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
