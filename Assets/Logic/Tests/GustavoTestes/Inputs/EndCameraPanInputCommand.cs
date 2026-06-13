using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.Services.CommandFactory;

public class EndCameraPanInputCommand : BaseCommand, ICommandVoid
{
    ICameraFocusService _cameraFocus;

    public override void ResolveDependencies() =>
        _cameraFocus = _diContainer.Resolve<ICameraFocusService>();

    public void Execute() => _cameraFocus?.EndPan();
}
