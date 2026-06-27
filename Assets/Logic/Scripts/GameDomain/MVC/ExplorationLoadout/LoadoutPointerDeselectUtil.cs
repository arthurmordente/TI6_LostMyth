using System.Collections.Generic;
using Logic.Scripts.GameDomain.Services.Cheats;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout
{
    /// <summary>
    /// Detects whether a screen pointer hit a loadout skill frame with bound data.
    /// </summary>
    public static class LoadoutPointerDeselectUtil
    {
        static readonly List<RaycastResult> Hits = new List<RaycastResult>(24);

        public static bool PointerHitsBoundSkill(Vector2 screenPosition)
        {
            return PointerHitsLoadoutInteractive(screenPosition);
        }

        public static bool PointerHitsLoadoutInteractive(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            Hits.Clear();
            var eventData = new PointerEventData(eventSystem) { position = screenPosition };
            eventSystem.RaycastAll(eventData, Hits);

            for (int i = 0; i < Hits.Count; i++)
            {
                GameObject hit = Hits[i].gameObject;
                if (hit == null) continue;

                if (hit.GetComponentInParent<LoadoutBookmarkHitTarget>() != null)
                    return true;

                if (hit.GetComponentInParent<LoadoutDetailPanelHitTarget>() != null)
                    return true;

                LoadoutSkillFrameView skillFrame = hit.GetComponentInParent<LoadoutSkillFrameView>();
                if (skillFrame != null && skillFrame.BoundSkill != null)
                    return true;

                LoadoutCheatFrameView cheatFrame = hit.GetComponentInParent<LoadoutCheatFrameView>();
                if (cheatFrame != null && cheatFrame.BoundCheat != null)
                    return true;
            }

            return false;
        }
    }
}
