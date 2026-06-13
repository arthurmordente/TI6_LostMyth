using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.Services.CommandFactory;

public class BeginCameraPanInputCommand : BaseCommand, ICommandVoid
{
    ICameraFocusService _cameraFocus;

    public override void ResolveDependencies() =>
        _cameraFocus = _diContainer.Resolve<ICameraFocusService>();

    public void Execute() => _cameraFocus?.BeginPan();
}
