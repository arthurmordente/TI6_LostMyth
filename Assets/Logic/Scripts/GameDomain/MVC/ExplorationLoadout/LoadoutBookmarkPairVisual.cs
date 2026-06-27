using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout
{
    public static class LoadoutBookmarkPairVisual
    {
        public static void ApplyPair(Button selected, Button unselected, float inactiveAlpha)
        {
            if (unselected != null)
            {
                if (selected != null)
                {
                    int behindIndex = Mathf.Min(
                        selected.transform.GetSiblingIndex(),
                        unselected.transform.GetSiblingIndex());
                    unselected.transform.SetSiblingIndex(behindIndex);
                }

                SetAlpha(unselected, inactiveAlpha);
            }

            if (selected != null)
            {
                selected.transform.SetAsLastSibling();
                SetAlpha(selected, 1f);
            }
        }

        static void SetAlpha(Button bookmark, float alpha)
        {
            if (bookmark == null) return;

            CanvasGroup group = bookmark.GetComponent<CanvasGroup>();
            if (group == null)
                group = bookmark.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;

            if (alpha >= 0.99f && !bookmark.interactable)
            {
                bookmark.interactable = true;
                bookmark.targetGraphic?.CrossFadeColor(bookmark.colors.normalColor, 0f, true, true);
            }
        }
    }
}
