using System;
using System.Threading;
using Logic.Scripts.Core.Mvc.LoadingScreen;
using Logic.Scripts.Services.Logger.Base;
using Logic.Scripts.Services.SceneServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Logic.Scripts.Services.StateMachineService {
    public class StateMachineService : IStateMachineService {
        private readonly ILoadingScreenController _loadingScreenController;
        private IGameState _currentGameState;

        public StateMachineService(ILoadingScreenController loadingScreenController) {
            _loadingScreenController = loadingScreenController;
        }

        public IGameState CurrentState() {
            return _currentGameState;
        }

        public async Awaitable EnterInitialGameState(IGameState initialState, CancellationTokenSource cancellationTokenSource) {
            _currentGameState = initialState;
            await _currentGameState.LoadState(cancellationTokenSource);
            await _currentGameState.StartState(cancellationTokenSource);
        }

        public void SwitchState(IGameState newState) {
            _ = SwitchStateAsync(newState);
        }

        private async Awaitable SwitchStateAsync(IGameState newState) {
            try {
                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);

                if (_currentGameState == null) {
                    LogService.LogError("No state to switch from, need to initialize a game state first!");
                    return;
                }

                _loadingScreenController.ShowTransitionTip();
                await _currentGameState.ExitState(cancellationTokenSource);
                _currentGameState = newState;
                await _currentGameState.LoadState(cancellationTokenSource);
                await SceneTransitionReadinessUtility.WaitUntilSceneReady(
                    SceneManager.GetActiveScene(), cancellationTokenSource.Token);
                _loadingScreenController.EnableContinuePrompt();
                await _loadingScreenController.WaitForPlayerContinue(cancellationTokenSource);
                _loadingScreenController.Hide();
                await _currentGameState.StartState(cancellationTokenSource);
            }
            catch (OperationCanceledException) {
                LogService.Log("Switching state operation was cancelled");
            }
            catch (Exception e) {
                LogService.LogError(e.Message);
                throw;
            }
        }
    }
}
