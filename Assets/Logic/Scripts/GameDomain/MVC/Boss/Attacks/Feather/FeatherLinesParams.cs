using System;

namespace Logic.Scripts.GameDomain.MVC.Boss.Attacks.Feather
{
    public enum FeatherAxisMode { X, Z, XZ, Diagonal }

    [Serializable]
    public struct FeatherLinesParams
    {
        public int featherCount;
        public FeatherAxisMode axisMode;
        /// <summary>Half-length of each strip along its axis (meters from arena center toward each edge). Full span = 2× this value.</summary>
        public float stripHalfExtent;
        /// <summary>Uniform XZ scale for catalog strip mesh (0 = default 1). Mesh at 1 = full arena.</summary>
        public float telegraphStripUniformScale;
        public float width;
        public float margin;
        public float forceBase;
        public float forcePerMeter;
        public float forcePerDebuff;
    }
}


