using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Shared
{
    /// <summary>
    /// Applies skill paint / frame / icon layers on a HUD or loadout slot hierarchy.
    /// Auto-resolves <c>img_Paint</c>, <c>img_Shape</c>, <c>img_Icon</c> / <c>icon_Skill</c>.
    /// </summary>
    public sealed class SkillSlotVisualView : MonoBehaviour
    {
        [SerializeField] private Image _backgroundPaint;
        [SerializeField] private Image _frameImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private SkillFrameVisualMode _visualMode = SkillFrameVisualMode.FullLayers;
        [SerializeField] private bool _iconPreserveAspect = true;

        public void Apply(SkillDataSO skill, ISkillVisualCatalog catalog, SkillFrameVisualMode? visualModeOverride = null)
        {
            ResolveReferences();
            var layout = new SkillSlotVisualApplicator.IconLayoutOptions { PreserveAspect = _iconPreserveAspect };
            SkillSlotVisualApplicator.Apply(
                skill,
                catalog,
                visualModeOverride ?? _visualMode,
                _backgroundPaint,
                _frameImage,
                _iconImage,
                layout);
        }

        public void Clear() => SkillSlotVisualApplicator.Clear(_backgroundPaint, _frameImage, _iconImage);

        void ResolveReferences()
        {
            if (_backgroundPaint == null) _backgroundPaint = FindChildImage("img_Paint");
            if (_frameImage == null)
            {
                _frameImage = FindChildImage("img_Shape");
                if (_frameImage == null && TryGetComponent(out Image selfImage))
                    _frameImage = selfImage;
            }
            if (_iconImage == null)
                _iconImage = FindChildImage("img_Icon") ?? FindChildImage("icon_Skill");
        }

        static Image FindChildImage(Transform root, string childName)
        {
            Transform child = root.Find(childName);
            if (child == null)
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
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

        Image FindChildImage(string childName) => FindChildImage(transform, childName);
    }
}
