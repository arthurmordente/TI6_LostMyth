using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExplorationSkillCatalogItemView : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image _iconImage;

    private SkillDataSO _boundSkill;
    private Action<SkillDataSO> _onSelected;
    private Action<SkillDataSO> _onHovered;

    public void Bind(SkillDataSO skill, Action<SkillDataSO> onSelected, Action<SkillDataSO> onHovered)
    {
        _boundSkill = skill;
        _onSelected = onSelected;
        _onHovered = onHovered;

        if (_nameText != null) _nameText.SetText(skill != null ? skill.SkillName : string.Empty);
        if (_iconImage != null)
        {
            _iconImage.sprite = skill != null ? skill.Icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onHovered?.Invoke(_boundSkill);
    }

    private void OnClick()
    {
        _onSelected?.Invoke(_boundSkill);
    }
}
