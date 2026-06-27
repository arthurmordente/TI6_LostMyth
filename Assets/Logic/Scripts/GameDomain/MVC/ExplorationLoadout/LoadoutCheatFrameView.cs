using System;
using Logic.Scripts.GameDomain.Services.Cheats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout
{
    public class LoadoutCheatFrameView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _button;
        [SerializeField] private Image _selectionOutline;

        CheatDataSO _boundCheat;
        Action<CheatDataSO> _onSelected;

        public CheatDataSO BoundCheat => _boundCheat;

        void Awake()
        {
            PrepareForCatalogHost();
            StripNestedCanvasIfPresent();
        }

        public void Bind(CheatDataSO cheat, Action<CheatDataSO> onSelected)
        {
            _boundCheat = cheat;
            _onSelected = onSelected;
            PrepareForCatalogHost();
            StripNestedCanvasIfPresent();
            ResolveReferences();
            ApplyIcon(cheat);
            SetSelected(false);
            WireButton();
        }

        public void SetSelected(bool selected)
        {
            if (_selectionOutline != null)
                _selectionOutline.enabled = selected;
        }

        public void SetEnabledState(bool enabled)
        {
            if (_iconImage == null) return;
            _iconImage.color = enabled
                ? Color.white
                : new Color(1f, 1f, 1f, 0.55f);
        }

        void PrepareForCatalogHost()
        {
            if (transform is RectTransform rt)
                rt.localScale = Vector3.one;
        }

        void StripNestedCanvasIfPresent()
        {
            Canvas nestedCanvas = GetComponent<Canvas>();
            if (nestedCanvas == null) return;

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster);

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null) Destroy(scaler);

            Destroy(nestedCanvas);
        }

        void ApplyIcon(CheatDataSO cheat)
        {
            if (_iconImage == null) return;
            _iconImage.sprite = cheat != null ? cheat.Icon : null;
            _iconImage.enabled = cheat != null && cheat.Icon != null;
        }

        void WireButton()
        {
            if (_button == null) return;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onSelected?.Invoke(_boundCheat));
        }

        void ResolveReferences()
        {
            if (_button == null) _button = GetComponentInChildren<Button>(true);
            if (_iconImage == null) _iconImage = FindChildImage("img_Icon") ?? FindChildImage("Icon");
            if (_selectionOutline == null)
                _selectionOutline = FindChildImage("img_SelectionOutline")
                    ?? FindChildImage("SelectionOutline");
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
    }
}
