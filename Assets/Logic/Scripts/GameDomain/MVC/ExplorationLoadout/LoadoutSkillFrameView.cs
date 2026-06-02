using System;
using DG.Tweening;
using Logic.Scripts.GameDomain.Services.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout
{
    /// <summary>
    /// Skill frame for loadout catalog and equipped slots: visuals + button click/hover.
    /// Wire <c>img_Paint</c>, <c>img_Shape</c>, <c>img_Icon</c> and the frame <see cref="Button"/> in the Inspector.
    /// </summary>
    public class LoadoutSkillFrameView : MonoBehaviour
    {
        [SerializeField] private Image _backgroundPaint;
        [SerializeField] private Image _shapeImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _button;
        [SerializeField] private Image _catalogSelectionOutline;
        [SerializeField] private TMP_Text _skillNameText;

        SkillDataSO _boundSkill;
        Action<SkillDataSO> _onSelected;
        Action<SkillDataSO> _onHovered;
        Outline _selectionOutlineEffect;

        private void Awake()
        {
            ResolveReferences();
            HideSkillNameLabel();
            PrepareForCatalogHost();
        }

        public SkillDataSO BoundSkill => _boundSkill;

        public void Bind(SkillDataSO skill, ISkillVisualCatalog catalog,
            Action<SkillDataSO> onSelected, Action<SkillDataSO> onHovered = null)
        {
            _boundSkill = skill;
            _onSelected = onSelected;
            _onHovered = onHovered;

            ApplySkillVisual(skill, catalog);
            SetCatalogSelected(false);
            WireButton();
        }

        public void SetCatalogSelected(bool selected) => SetSelectionOutline(selected);

        public void SetSlotSelected(bool selected) => SetSelectionOutline(selected);

        void SetSelectionOutline(bool selected)
        {
            EnsureSelectionOutline();
            if (_catalogSelectionOutline != null)
                _catalogSelectionOutline.enabled = selected;
            if (_selectionOutlineEffect != null)
                _selectionOutlineEffect.enabled = selected;
        }

        void EnsureSelectionOutline()
        {
            if (_catalogSelectionOutline == null)
            {
                _catalogSelectionOutline = FindChildImage("img_SelectionOutline")
                    ?? FindChildImage("SelectionOutline")
                    ?? FindChildImage("Selected");
            }

            if (_selectionOutlineEffect == null && _shapeImage != null)
            {
                _selectionOutlineEffect = _shapeImage.GetComponent<Outline>();
                if (_selectionOutlineEffect == null)
                    _selectionOutlineEffect = _shapeImage.gameObject.AddComponent<Outline>();
                _selectionOutlineEffect.effectColor = new Color(1f, 0.85f, 0.2f, 0.95f);
                _selectionOutlineEffect.effectDistance = new Vector2(3f, -3f);
                _selectionOutlineEffect.enabled = false;
            }

            if (_catalogSelectionOutline != null)
                _catalogSelectionOutline.enabled = false;
        }

        public void PlayInvalidAssignShake()
        {
            if (transform is not RectTransform rect) return;
            DOTween.Kill(rect, true);
            rect.DOShakeAnchorPos(0.35f, strength: 18f, vibrato: 16, randomness: 60f, fadeOut: true);
        }

        public void ApplySkillVisual(SkillDataSO skill, ISkillVisualCatalog catalog)
        {
            if (skill == null)
            {
                ClearVisual();
                return;
            }

            if (catalog != null && catalog.TryGetLayerSprites(skill.Divinity, skill.SkillType, out Sprite bg, out Sprite frame))
            {
                SetImage(_backgroundPaint, bg);
                SetImage(_shapeImage, frame);
            }
            else
            {
                SetImage(_backgroundPaint, null);
                SetImage(_shapeImage, null);
            }

            SetImage(_iconImage, skill.Icon);
        }

        public void ClearVisual()
        {
            SetImage(_backgroundPaint, null);
            SetImage(_shapeImage, null);
            SetImage(_iconImage, null);
        }

        void ResolveReferences()
        {
            if (_backgroundPaint == null) _backgroundPaint = FindChildImage("img_Paint");
            if (_shapeImage == null) _shapeImage = FindChildImage("img_Shape");
            if (_iconImage == null) _iconImage = FindChildImage("img_Icon");
            if (_button == null) _button = GetComponentInChildren<Button>(true);
        }

        void HideSkillNameLabel()
        {
            if (_skillNameText != null)
            {
                _skillNameText.gameObject.SetActive(false);
                return;
            }

            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null) continue;
                string objectName = text.gameObject.name;
                if (objectName.Equals("Name", StringComparison.OrdinalIgnoreCase)
                    || objectName.Equals("SkillName", StringComparison.OrdinalIgnoreCase)
                    || objectName.Equals("txt_Name", StringComparison.OrdinalIgnoreCase)
                    || objectName.Equals("txt_SkillName", StringComparison.OrdinalIgnoreCase))
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>SkillFrame.prefab root may ship at scale zero for layout authoring; restore visible scale when spawned.</summary>
        void PrepareForCatalogHost()
        {
            if (transform is RectTransform rt)
                rt.localScale = Vector3.one;
        }

        void WireButton()
        {
            if (_button == null) return;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onSelected?.Invoke(_boundSkill));

            EventTrigger trigger = _button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = _button.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerEnter);

            if (_onHovered == null) return;

            var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEntry.callback.AddListener(_ => _onHovered?.Invoke(_boundSkill));
            trigger.triggers.Add(hoverEntry);
        }

        Image FindChildImage(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == childName)
                    {
                        child = t;
                        break;
                    }
                }
            }

            return child != null ? child.GetComponent<Image>() : null;
        }

        static void SetImage(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
            if (sprite != null) image.color = Color.white;
        }
    }
}
