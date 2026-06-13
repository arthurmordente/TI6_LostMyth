namespace Logic.Scripts.Core.Mvc.WorldCamera
{
    public readonly struct CameraFocusHandle
    {
        public static CameraFocusHandle Invalid => new CameraFocusHandle(0);

        public int Id { get; }
        public bool IsValid => Id > 0;

        public CameraFocusHandle(int id) => Id = id;
    }
}
