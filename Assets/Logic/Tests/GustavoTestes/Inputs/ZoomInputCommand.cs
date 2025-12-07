using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Core.Mvc.WorldCamera;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZoomInputCommand : BaseCommand, ICommandVoid
{
    private IWorldCameraController _worldCameraController;

    private const float StepPerNotch = 0.5f;

    public override void ResolveDependencies()
    {
        _worldCameraController = _diContainer.Resolve<IWorldCameraController>();
    }

    public void Execute()
    {
        if (_worldCameraController == null || Mouse.current == null) return;

        float dy = Mouse.current.scroll.ReadValue().y * -1f;
        if (Mathf.Abs(dy) < 0.01f) return;

        float step = Mathf.Sign(dy) * StepPerNotch;
        _worldCameraController.AdjustZoom(step);
    }
}
