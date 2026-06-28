using System;
using UnityEngine;

namespace Logic.Scripts.Core.Mvc.WorldCamera
{
    [Serializable]
    public struct SceneCameraEntrySettings
    {
        public bool OverrideDefaults;
        public float HorizontalAngle;
        public float VerticalAngle;
        public float OrbitHeight;
        public float OrbitRadius;
        public Vector3 PanOffset;
        public float BlendDuration;

        public static SceneCameraEntrySettings FromCurrentDefaults() => new SceneCameraEntrySettings
        {
            OverrideDefaults = false,
            HorizontalAngle = 0f,
            VerticalAngle = 20f,
            OrbitHeight = 12f,
            OrbitRadius = 9.5f,
            PanOffset = Vector3.zero,
            BlendDuration = 0.4f
        };

        public static SceneCameraEntrySettings ExplorationDefaults() => FromCurrentDefaults();

        public static SceneCameraEntrySettings GameplayFightDefaults() => FromCurrentDefaults();
    }
}
