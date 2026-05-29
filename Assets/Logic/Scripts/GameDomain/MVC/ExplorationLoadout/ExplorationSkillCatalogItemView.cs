using System;
using Logic.Scripts.GameDomain.Services.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExplorationSkillCatalogItemView : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image _frameImage;
    [SerializeField] private Image _iconImage;

    private SkillDataSO _boundSkill;
    private Action<SkillDataSO> _onSelected;
    private Action<SkillDataSO> _onHovered;

    public void ConfigureRuntimeReferences(Button button, Image frameImage, Image iconImage, TMP_Text nameText = null)
    {
        _button = button;
        _frameImage = frameImage;
        _iconImage = iconImage;
        _nameText = nameText;
    }

    public void Bind(SkillDataSO skill, ISkillVisualCatalog catalog,
        Action<SkillDataSO> onSelected, Action<SkillDataSO> onHovered)
    {
        _boundSkill = skill;
        _onSelected = onSelected;
        _onHovered = onHovered;

        if (_nameText != null)
            _nameText.SetText(skill != null ? skill.SkillName : string.Empty);

        if (skill == null) {
            ClearFrameAndIcon();
        } else {
            if (_frameImage != null) {
                if (catalog != null && catalog.TryGetLayerSprites(skill.Divinity, skill.SkillType, out _, out var frame)) {
                    _frameImage.sprite = frame;
                    _frameImage.enabled = frame != null;
                    if (frame != null) _frameImage.color = Color.white;
                } else {
                    _frameImage.sprite = null;
                    _frameImage.enabled = false;
                }
            }

            if (_iconImage != null) {
                _iconImage.sprite = skill.Icon;
                _iconImage.enabled = skill.Icon != null;
                if (skill.Icon != null) _iconImage.color = Color.white;
            }
        }

        if (_button != null) {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
        }
    }

    private void ClearFrameAndIcon()
    {
        if (_frameImage != null) {
            _frameImage.sprite = null;
            _frameImage.enabled = false;
        }
        if (_iconImage != null) {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
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
