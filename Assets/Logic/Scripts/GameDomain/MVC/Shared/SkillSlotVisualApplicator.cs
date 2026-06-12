using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Shared
{
    public static class SkillSlotVisualApplicator
    {
        public struct IconLayoutOptions
        {
            public bool PreserveAspect;
            public bool OverrideAnchoredPosition;
            public Vector2 AnchoredPosition;
            public bool OverrideSizeDelta;
            public Vector2 SizeDelta;
        }

        public static void Apply(
            SkillDataSO skill,
            ISkillVisualCatalog catalog,
            SkillFrameVisualMode visualMode,
            Image backgroundPaint,
            Image frame,
            Image icon,
            IconLayoutOptions iconLayout = default)
        {
            if (skill == null)
            {
                Clear(backgroundPaint, frame, icon);
                return;
            }

            bool showPaint = visualMode == SkillFrameVisualMode.FullLayers;
            if (showPaint && catalog != null
                && catalog.TryGetLayerSprites(skill.Divinity, skill.SkillType, out Sprite bg, out Sprite frameSprite))
            {
                SetImage(backgroundPaint, bg);
                SetImage(frame, frameSprite);
            }
            else if (catalog != null
                && catalog.TryGetLayerSprites(skill.Divinity, skill.SkillType, out _, out Sprite frameOnly))
            {
                SetImage(backgroundPaint, null, hideWhenEmpty: true);
                SetImage(frame, frameOnly);
            }
            else
            {
                SetImage(backgroundPaint, null, hideWhenEmpty: true);
                SetImage(frame, null);
            }

            if (!showPaint && backgroundPaint != null)
            {
                backgroundPaint.sprite = null;
                backgroundPaint.enabled = false;
            }

            SetImage(icon, skill.Icon);
            ApplyIconLayout(icon, iconLayout);
        }

        public static void Clear(Image backgroundPaint, Image frame, Image icon)
        {
            SetImage(backgroundPaint, null, hideWhenEmpty: true);
            SetImage(frame, null);
            SetImage(icon, null);
        }

        static void ApplyIconLayout(Image icon, IconLayoutOptions layout)
        {
            if (icon == null) return;

            icon.preserveAspect = layout.PreserveAspect;

            if (icon.transform is not RectTransform rt) return;

            if (layout.OverrideAnchoredPosition)
                rt.anchoredPosition = layout.AnchoredPosition;

            if (layout.OverrideSizeDelta)
                rt.sizeDelta = layout.SizeDelta;
        }

        static void SetImage(Image image, Sprite sprite, bool hideWhenEmpty = false)
        {
            if (image == null) return;
            image.sprite = sprite;
            if (hideWhenEmpty)
                image.enabled = sprite != null;
            else
                image.enabled = sprite != null;
            if (sprite != null) image.color = Color.white;
        }
    }
}
