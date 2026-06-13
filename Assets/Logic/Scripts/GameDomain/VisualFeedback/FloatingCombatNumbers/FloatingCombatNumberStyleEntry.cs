using System;
using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers
{
    [Serializable]
    public class FloatingCombatNumberStyleEntry
    {
        public FloatingCombatNumberKind Kind = FloatingCombatNumberKind.Damage;
        public Color TextColor = Color.white;
        [Tooltip("Absolute value at or below which MinFontSize is used.")]
        public float MinValue = 1f;
        [Tooltip("Absolute value at or above which MaxFontSize is used.")]
        public float MaxValue = 50f;
        public float MinFontSize = 2.5f;
        public float MaxFontSize = 6f;
        public bool ShowPlusSignForHeal = true;
    }
}
