using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Core.Mvc.WorldCamera;
using UnityEngine;

public class RotateCamInputCommand : BaseCommand, ICommandVoid {
    private IWorldCameraController _WorldCameraController;
    private GameInputActions _gameInputActions;

    public override void ResolveDependencies() {
        _WorldCameraController = _diContainer.Resolve<IWorldCameraController>();
        _gameInputActions = _diContainer.Resolve<GameInputActions>();
    }

    public void Execute() {
        
    }
}
