using Logic.Scripts.Core.CoreInitiator.Base;
using Logic.Scripts.GameDomain.Commands;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.InitiatorInvokerService;
using System.Threading;
using UnityEngine;

namespace Logic.Scripts.GameDomain.GameplayInitiator {
    public class GamePlayInitiator : ISceneInitiator, IGamePlayInitiator {
        private readonly ICommandFactory _commandFactory;
        private readonly ISceneInitiatorsService _sceneInitiatorsService;

        public SceneType SceneType => SceneType.GamePlayScene;

        public GamePlayInitiator(ICommandFactory commandFactory, ISceneInitiatorsService sceneInitiatorsService) {
            _commandFactory = commandFactory;
            _sceneInitiatorsService = sceneInitiatorsService;
            _sceneInitiatorsService.RegisterInitiator(this);
        }

        public async Awaitable LoadEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            var enterData = (GamePlayInitatorEnterData)enterDataObject;
            await _commandFactory.CreateCommandAsync<LoadGamePlayStateCommand>().SetEnterData(enterData).Execute(cancellationTokenSource);
        }

        public async Awaitable StartEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            var enterData = (GamePlayInitatorEnterData)enterDataObject;
            await _commandFactory.CreateCommandAsync<StartGamePlayStateCommand>().SetEnterData(enterData).Execute(cancellationTokenSource);
        }

        public async Awaitable InitExitPoint(CancellationTokenSource cancellationTokenSource) {
            await Awaitable.NextFrameAsync(cancellationTokenSource.Token);
            _sceneInitiatorsService.UnregisterInitiator(this);
            _commandFactory.CreateCommandVoid<ExitGamePlayStateCommand>().Execute();
        }
    }
}