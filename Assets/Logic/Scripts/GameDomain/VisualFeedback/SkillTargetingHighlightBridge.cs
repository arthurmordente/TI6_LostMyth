using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback
{
    /// <summary>
    /// Resolves <see cref="SkillTargetingHighlightPresenter"/> under <see cref="IEffectable.GetReferenceTransform"/>.
    /// All <see cref="IEffectable"/> implementations can delegate here with one line; add the presenter + material on prefabs that should show fresnel.
    /// </summary>
    public static class SkillTargetingHighlightBridge
    {
        public static void SetHighlighted(IEffectable effectable, bool active)
        {
            if (effectable == null) return;
            Transform t = effectable.GetReferenceTransform();
            if (t == null) return;
            var presenter = t.GetComponent<SkillTargetingHighlightPresenter>()
                            ?? t.GetComponentInChildren<SkillTargetingHighlightPresenter>(true);
            presenter?.SetHighlighted(active);
        }
    }
}
