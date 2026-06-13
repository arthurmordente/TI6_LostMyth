using DG.Tweening;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Shared
{
    /// <summary>Shared uGUI shake presets (loadout invalid slot, combat cast blocked, etc.).</summary>
    public static class UiRectShake
    {
        public static void PlayInvalidSlotShake(RectTransform rect)
        {
            if (rect == null) return;
            DOTween.Kill(rect, true);
            rect.DOShakeAnchorPos(0.35f, strength: 18f, vibrato: 16, randomness: 60f, fadeOut: true);
        }
    }
}
