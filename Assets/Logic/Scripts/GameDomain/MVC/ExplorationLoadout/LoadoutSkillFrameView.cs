using System;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout
{
    /// <summary>
    /// Skill frame for the loadout catalog: visuals + button click/hover.
    /// Wire <c>img_Paint</c>, <c>img_Shape</c>, <c>img_Icon</c> and the frame <see cref="Button"/> in the Inspector.
    /// </summary>
    public class LoadoutSkillFrameView : MonoBehaviour
    {
        [SerializeField] private Image _backgroundPaint;
        [SerializeField] private Image _shapeImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _button;
        [SerializeField] private Image _catalogSelectionOutline;

        SkillDataSO _boundSkill;
        Action<SkillDataSO> _onSelected;
        Action<SkillDataSO> _onHovered;

        private void Awake()
        {
            ResolveReferences();
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

        public void SetCatalogSelected(bool selected)
        {
            if (_catalogSelectionOutline != null)
                _catalogSelectionOutline.enabled = selected;
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

        /// <summary>SkillFrame.prefab root may ship at scale zero for layout authoring; restore visible scale when spawned in the catalog.</summary>
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
