using Logic.Scripts.Core.Mvc.LoadingScreen;
using Logic.Scripts.Core.Mvc.UICamera;
using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.Services.AddressablesLoader;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.InitiatorInvokerService;
using Logic.Scripts.Services.Logger;
using Logic.Scripts.Services.ResourcesLoaderService;
using Logic.Scripts.Services.StateMachineService;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;
using Zenject;

public class CoreInstaler : MonoInstaller
{
    [SerializeField] private UpdateSubscriptionService _updateSubscriptionService;
    [SerializeField] private AudioService _audioService;
    [SerializeField] private LoadingScreenCanvasView _loadingScreenView;
    [SerializeField] private LoadingTipPoolSO _loadingTipPool;
    [SerializeField] private UICameraView _uiCameraView;
    [SerializeField] private WorldCameraView _worldCameraView;
    
    public override void InstallBindings() {
        Container.BindInterfacesTo<UnityLogger>().AsSingle().NonLazy();
        Container.BindInterfacesTo<SceneLoaderService>().AsSingle().NonLazy();
        Container.BindInterfacesTo<AddressablesLoaderService>().AsSingle().NonLazy();
        Container.BindInterfacesTo<ResourcesLoaderService>().AsSingle().NonLazy();
        Container.BindInterfacesTo<StateMachineService>().AsSingle().NonLazy();
        Container.BindInterfacesTo<UpdateSubscriptionService>().FromInstance(_updateSubscriptionService).AsSingle().NonLazy();
        Container.BindInterfacesTo<AudioService>().FromInstance(_audioService).AsSingle().NonLazy();
        Container.BindInterfacesTo<SceneInitiatorsService>().AsSingle().NonLazy();
        Container.BindInterfacesTo<CommandFactory>().AsSingle().CopyIntoAllSubContainers().NonLazy();
        Container.BindInterfacesTo<LoadingScreenController>().AsSingle().WithArguments(_loadingScreenView).NonLazy();
        if (_loadingTipPool != null)
            Container.Bind<LoadingTipPoolSO>().FromInstance(_loadingTipPool).AsSingle();
        Container.BindInterfacesTo<UICameraController>().AsSingle().WithArguments(_uiCameraView).NonLazy();
        Container.BindInterfacesTo<WorldCameraController>().AsSingle().WithArguments(_worldCameraView).NonLazy();
        Container.BindInterfacesTo<CameraFocusService>().AsSingle().NonLazy();
        Container.Bind<GameInputActions>().AsSingle().NonLazy();
    }
}
