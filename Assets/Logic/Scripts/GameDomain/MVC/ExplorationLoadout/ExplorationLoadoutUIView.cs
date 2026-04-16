using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logic.Scripts.GameDomain.Services.Skills;

public class ExplorationLoadoutUIView : MonoBehaviour
{
    [Serializable]
    public class SkillSlotUiRef
    {
        public Button Button;
        public TMP_Text NameText;
        public TMP_Text CostText;
        public Image IconImage;
        public Image SelectionOutline;
    }

    [Header("Root")]
    [SerializeField] private GameObject _rootPanel;
    [SerializeField] private Button _closeButton;

    [Header("Loadout Slots")]
    [SerializeField] private List<SkillSlotUiRef> _playerSlots = new List<SkillSlotUiRef>(4);
    [SerializeField] private List<SkillSlotUiRef> _bookSlots = new List<SkillSlotUiRef>(4);

    [Header("Catalog")]
    [SerializeField] private Transform _catalogContainer;
    [SerializeField] private ExplorationSkillCatalogItemView _catalogItemPrefab;

    [Header("Details")]
    [SerializeField] private TMP_Text _detailNameText;
    [SerializeField] private TMP_Text _detailDescriptionText;
    [SerializeField] private TMP_Text _detailPowerText;
    [SerializeField] private TMP_Text _detailCostText;
    [SerializeField] private TMP_Text _detailRangeText;
    [SerializeField] private TMP_Text _detailCooldownText;
    [SerializeField] private Image _detailIconImage;

    public bool IsVisible => _rootPanel != null && _rootPanel.activeSelf;

    public void Init()
    {
        SetVisible(false);
    }

    public void RegisterCallbacks(Action onClose, Action<int> onPlayerSlotClicked, Action<int> onBookSlotClicked)
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(() => onClose?.Invoke());
        }

        RegisterSlotCallbacks(_playerSlots, onPlayerSlotClicked);
        RegisterSlotCallbacks(_bookSlots, onBookSlotClicked);
    }

    public void SetVisible(bool visible)
    {
        if (_rootPanel != null) _rootPanel.SetActive(visible);
    }

    public void SetSlotData(SkillLoadoutUnitType unitType, int slotIndex, SkillDataSO skill)
    {
        SkillSlotUiRef slot = GetSlot(unitType, slotIndex);
        if (slot == null) return;

        if (slot.NameText != null) slot.NameText.SetText(skill != null ? skill.SkillName : "-");
        if (slot.CostText != null) slot.CostText.SetText(skill != null ? skill.Cost.ToString() : "0");
        if (slot.IconImage != null)
        {
            slot.IconImage.sprite = skill != null ? skill.Icon : null;
            slot.IconImage.enabled = slot.IconImage.sprite != null;
        }
    }

    public void SetSelectedSlot(SkillLoadoutUnitType unitType, int slotIndex)
    {
        SetSelectionState(_playerSlots, false);
        SetSelectionState(_bookSlots, false);

        SkillSlotUiRef selected = GetSlot(unitType, slotIndex);
        if (selected?.SelectionOutline != null) selected.SelectionOutline.enabled = true;
    }

    public void ClearCatalog()
    {
        if (_catalogContainer == null) return;
        for (int i = _catalogContainer.childCount - 1; i >= 0; i--)
            Destroy(_catalogContainer.GetChild(i).gameObject);
    }

    public ExplorationSkillCatalogItemView CreateCatalogItem()
    {
        if (_catalogContainer == null || _catalogItemPrefab == null) return null;
        return Instantiate(_catalogItemPrefab, _catalogContainer);
    }

    public void ShowSkillDetails(SkillDataSO skill)
    {
        if (_detailNameText != null) _detailNameText.SetText(skill != null ? skill.SkillName : "-");
        if (_detailDescriptionText != null) _detailDescriptionText.SetText(skill != null ? skill.Description : string.Empty);
        if (_detailPowerText != null) _detailPowerText.SetText(skill != null ? skill.Power.ToString() : "-");
        if (_detailCostText != null) _detailCostText.SetText(skill != null ? skill.Cost.ToString() : "-");
        if (_detailRangeText != null) _detailRangeText.SetText(skill != null ? skill.Range.ToString("0.##") : "-");
        if (_detailCooldownText != null) _detailCooldownText.SetText(skill != null ? skill.CoolDown.ToString("0.##") : "-");
        if (_detailIconImage != null)
        {
            _detailIconImage.sprite = skill != null ? skill.Icon : null;
            _detailIconImage.enabled = _detailIconImage.sprite != null;
        }
    }

    private static void RegisterSlotCallbacks(List<SkillSlotUiRef> slots, Action<int> onClicked)
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Count; i++)
        {
            int index = i;
            var slot = slots[i];
            if (slot?.Button == null) continue;
            slot.Button.onClick.RemoveAllListeners();
            slot.Button.onClick.AddListener(() => onClicked?.Invoke(index));
        }
    }

    private static void SetSelectionState(List<SkillSlotUiRef> slots, bool selected)
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot?.SelectionOutline != null) slot.SelectionOutline.enabled = selected;
        }
    }

    private SkillSlotUiRef GetSlot(SkillLoadoutUnitType unitType, int slotIndex)
    {
        List<SkillSlotUiRef> slots = unitType == SkillLoadoutUnitType.Book ? _bookSlots : _playerSlots;
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Count) return null;
        return slots[slotIndex];
    }
}
