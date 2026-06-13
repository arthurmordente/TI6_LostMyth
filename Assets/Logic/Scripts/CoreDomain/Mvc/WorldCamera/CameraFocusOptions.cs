namespace Logic.Scripts.Core.Mvc.WorldCamera
{
    public struct CameraFocusOptions
    {
        public float BlendDuration;
        public bool SuppressRotation;
        public bool SuppressPan;

        public static CameraFocusOptions Cinematic(float blendSeconds = 0.5f) => new CameraFocusOptions
        {
            BlendDuration = blendSeconds,
            SuppressRotation = true,
            SuppressPan = true
        };

        public static CameraFocusOptions DefaultFollow => new CameraFocusOptions
        {
            BlendDuration = 0.4f,
            SuppressRotation = false,
            SuppressPan = false
        };
    }
}
