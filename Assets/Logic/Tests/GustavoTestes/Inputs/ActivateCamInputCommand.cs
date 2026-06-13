using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.Services.CommandFactory;
using UnityEngine;

public class ActivateCamInputCommand : BaseCommand, ICommandVoid {
    private IWorldCameraController _WorldCameraController;
    private ICameraFocusService _cameraFocus;

    public override void ResolveDependencies() {
        _WorldCameraController = _diContainer.Resolve<IWorldCameraController>();
        try { _cameraFocus = _diContainer.Resolve<ICameraFocusService>(); } catch { _cameraFocus = null; }
    }

    public void Execute() {
        if (_cameraFocus != null && (_cameraFocus.IsCinematicLockActive || _cameraFocus.IsPanActive))
            return;
        _WorldCameraController.UnlockCameraRotate();
    }
}
