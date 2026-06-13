using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.Commands;
using Logic.Scripts.GameDomain.MVC.Boss;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.UpdateService;
using System.Threading;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.StartLevel {
    public class StartLevelCommand : BaseCommand, ICommandAsync {

        private INaraController _naraController;
        private ICameraFocusService _cameraFocusService;
        private IUpdateSubscriptionService _updateSubscriptionService;
        private ICommandFactory _commandFactory;
        private IBossController _bossController;
        private ICastController _castController;

        public override void ResolveDependencies() {
            _naraController = _diContainer.Resolve<INaraController>();
            _cameraFocusService = _diContainer.Resolve<ICameraFocusService>();
            _updateSubscriptionService = _diContainer.Resolve<IUpdateSubscriptionService>();
            _commandFactory = _diContainer.Resolve<ICommandFactory>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            _cameraFocusService.SetDefaultFollow(_naraController.NaraViewGO.transform);
            await Awaitable.NextFrameAsync();
        }

        public StartLevelCommand StartBoss() {
            ResolveDependenciesBoss();
            _castController.InitEntryPoint(_naraController);
            _bossController.Initialize();
            _commandFactory.CreateCommandVoid<EnterTurnModeCommand>().Execute();
            return this;
        }

        private void ResolveDependenciesBoss() {
            _castController = _diContainer.Resolve<ICastController>();
            _bossController = _diContainer.Resolve<IBossController>();
        }
    }
}
