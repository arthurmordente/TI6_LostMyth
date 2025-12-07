using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.Logger.Base;
using Logic.Scripts.Utils;
using System.Threading;
using UnityEngine.InputSystem;
using UnityEngine;

namespace Logic.Scripts.GameDomain.GameInputActions {
    public class GameInputActionsController : IGameInputActionsController {
        private readonly global::GameInputActions _gameInputActions;
        private readonly ICommandFactory _commandFactory;

        public GameInputActionsController(global::GameInputActions gameInputActions, ICommandFactory commandFactory) {
            _gameInputActions = gameInputActions;
            _commandFactory = commandFactory;
        }

        public void EnableGameplayInputs() {
            LogService.LogTopic("EnableInputs", LogTopicType.Inputs);
            _gameInputActions.Player.Enable();
        }

        public void EnableUIInputs() {
            LogService.LogTopic("EnableUIInputs", LogTopicType.Inputs);
            _gameInputActions.UI.Enable();
        }

        public void EnableExplorationInputs() {
            LogService.LogTopic("EnableExplorationInputs", LogTopicType.Inputs);
            _gameInputActions.Exploration.Enable();
        }

        public void DisableGameplayInputs() {
            LogService.LogTopic("EnableInputs", LogTopicType.Inputs);
            _gameInputActions.Player.Disable();
        }

        public void DisableUIInputs() {
            LogService.LogTopic("EnableUIInputs", LogTopicType.Inputs);
            _gameInputActions.UI.Disable();
        }

        public void DisableExplorationInputs() {
            LogService.LogTopic("EnableExplorationInputs", LogTopicType.Inputs);
            _gameInputActions.Exploration.Disable();
        }

        #region gameplayInput
        public void RegisterGameplayInputListeners() {
            LogService.LogTopic("Register Gameplay input listeners", LogTopicType.Inputs);
            _gameInputActions.Player.ActivateCam.started += OnActivateCamAndCancelAbilityStarted;
            _gameInputActions.Player.ActivateCam.canceled += OnActivateCamAndCancelAbilityCanceled;
            _gameInputActions.Player.CreateCopy1.started += OnCreateCopy1Started;
            _gameInputActions.Player.CreateCopy2.started += OnCreateCopy2Started;
            _gameInputActions.Player.Interact.started += OnInteractStarted;
            _gameInputActions.Player.Move.started += OnMoveStarted;
            _gameInputActions.Player.Move.canceled += OnMoveCanceled;
            _gameInputActions.Player.PassTurn.started += OnPassTurnStarted;
            _gameInputActions.Player.Pause.started += OnPauseGameplayStarted;
            _gameInputActions.Player.ResetMovement.started += OnResetMovementStarted;
            _gameInputActions.Player.RotateCam.started += OnRotateCamStarted;
            _gameInputActions.Player.UseAbility1.started += OnUseAbility1Started;
            _gameInputActions.Player.UseAbility2.started += OnUseAbility2Started;
            _gameInputActions.Player.UseAbility3.started += OnUseAbility3Started;
            _gameInputActions.Player.UseAbility4.started += UseAbility4Started;
            _gameInputActions.Player.UseAbility5.started += UseAbility5Started;
            _gameInputActions.Player.MouseClick.started += OnMouseClickStarted;
            _gameInputActions.Player.Zoom.performed += OnZoomPerformed;
        }

        public void UnregisterGameplayInputListeners() {
            LogService.LogTopic("Unregister all input listeners", LogTopicType.Inputs);
            _gameInputActions.Player.ActivateCam.started -= OnActivateCamAndCancelAbilityStarted;
            _gameInputActions.Player.ActivateCam.canceled -= OnActivateCamAndCancelAbilityCanceled;

            _gameInputActions.Player.CreateCopy1.started -= OnCreateCopy1Started;
            _gameInputActions.Player.CreateCopy2.started -= OnCreateCopy2Started;
            _gameInputActions.Player.Move.started -= OnMoveStarted;
            _gameInputActions.Player.Move.canceled -= OnMoveCanceled;
            _gameInputActions.Player.PassTurn.started -= OnPassTurnStarted;
            _gameInputActions.Player.Pause.started -= OnPauseGameplayStarted;
            _gameInputActions.Player.ResetMovement.started -= OnResetMovementStarted;
            _gameInputActions.Player.RotateCam.started -= OnRotateCamStarted;
            _gameInputActions.Player.UseAbility1.started -= OnUseAbility1Started;
            _gameInputActions.Player.UseAbility2.started -= OnUseAbility2Started;
            _gameInputActions.Player.UseAbility3.started -= OnUseAbility3Started;
            _gameInputActions.Player.UseAbility4.started -= UseAbility4Started;
            _gameInputActions.Player.UseAbility5.started -= UseAbility5Started;

            _gameInputActions.Player.MouseClick.started -= OnMouseClickStarted;
            _gameInputActions.Player.Zoom.performed -= OnZoomPerformed;
        }

        private void OnMouseClickStarted(InputAction.CallbackContext context) {
            _commandFactory.CreateCommandVoid<MouseClickInputCommand>().Execute();
        }
        private void OnUseAbility1Started(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<UseAbility1InputCommand>().Execute();
        }
        private void OnUseAbility2Started(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<UseAbility2InputCommand>().Execute();
        }
        private void OnUseAbility3Started(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<UseAbility3InputCommand>().Execute();
        }
        private void UseAbility5Started(InputAction.CallbackContext context) {
            _commandFactory.CreateCommandVoid<UseAbility5InputCommand>().Execute();
        }
        private void UseAbility4Started(InputAction.CallbackContext context) {
            _commandFactory.CreateCommandVoid<UseAbility4InputCommand>().Execute();
        }
        private void OnRotateCamStarted(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<RotateCamInputCommand>().Execute();
        }
        private void OnResetMovementStarted(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<ResetTurnInputCommand>().Execute();
        }
        private void OnPauseGameplayStarted(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<PauseGameplayInputCommand>().Execute();
        }
        private void OnPassTurnStarted(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<PassTurnInputCommand>().Execute();
        }
        private void OnMoveStarted(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<MoveInputCommand>().Execute();
        }
        private void OnCreateCopy2Started(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<CreateCopy2InputCommand>().Execute();
        }
        private void OnCreateCopy1Started(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<CreateCopy1InputCommand>().Execute();
        }
        private void OnActivateCamAndCancelAbilityStarted(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<ActivateCamAndCancelAbilityInputCommand>().Execute();
        }
        private void OnMoveCanceled(InputAction.CallbackContext context) { _commandFactory.CreateCommandVoid<StopMoveInputCommand>().Execute(); }
        private void OnActivateCamAndCancelAbilityCanceled(InputAction.CallbackContext context) { _commandFactory.CreateCommandVoid<DeactivateCamInputCommand>().Execute(); }
        #endregion
        private void OnZoomPerformed(InputAction.CallbackContext context) {
            _commandFactory.CreateCommandVoid<ZoomInputCommand>().Execute();
        }

        #region explorationInput
        public void RegisterExplorationInputListeners() {
            LogService.LogTopic("Register all input listeners", LogTopicType.Inputs);
            _gameInputActions.Exploration.ActivateCam.started += OnActivateCamStarted;
            _gameInputActions.Exploration.ActivateCam.canceled += OnActivateCamCanceled;
            _gameInputActions.Exploration.Interact.started += OnInteractStarted;
            _gameInputActions.Exploration.Move.started += OnMoveStarted;
            _gameInputActions.Exploration.Move.canceled += OnMoveCanceled;
            _gameInputActions.Exploration.Pause.started += OnPauseExplorationStarted;
            _gameInputActions.Exploration.RotateCam.started += OnRotateCamStarted;
            _gameInputActions.Exploration.Zoom.started += OnZoomPerformed;
        }

        public void UnregisterExplorationInputListeners() {
            LogService.LogTopic("Register all input listeners", LogTopicType.Inputs);
            _gameInputActions.Exploration.ActivateCam.started -= OnActivateCamStarted;
            _gameInputActions.Exploration.ActivateCam.canceled -= OnActivateCamCanceled;
            _gameInputActions.Exploration.Interact.started -= OnInteractStarted;
            _gameInputActions.Exploration.Move.started -= OnMoveStarted;
            _gameInputActions.Exploration.Move.canceled -= OnMoveCanceled;
            _gameInputActions.Exploration.Pause.started -= OnPauseExplorationStarted;
            _gameInputActions.Exploration.RotateCam.started -= OnRotateCamStarted;
            _gameInputActions.Exploration.Zoom.started -= OnZoomPerformed;
        }

        private void OnActivateCamStarted(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<ActivateCamInputCommand>().Execute();
        }

        private void OnActivateCamCanceled(InputAction.CallbackContext context) {
            _commandFactory.CreateCommandVoid<DeactivateCamInputCommand>().Execute();
        }

        private void OnInteractStarted(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<InteractInputCommand>().Execute();
        }
        private void OnPauseExplorationStarted(InputAction.CallbackContext obj) {
            _commandFactory.CreateCommandVoid<PauseExplorationInputCommand>().Execute();
        }
        #endregion

        public async Awaitable WaitForAnyKeyPressed(CancellationTokenSource cancellationTokenSource, bool canPressOverGui = false) {
            await AwaitableUtils.WaitUntil(() => IsAnyInputPressed(), cancellationTokenSource.Token);
        }

        private bool IsAnyInputPressed() {
            return
                (Keyboard.current?.anyKey.wasPressedThisFrame == true) ||
                (Mouse.current?.leftButton.wasPressedThisFrame == true) ||
                (Mouse.current?.rightButton.wasPressedThisFrame == true) ||
                (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true);
        }
    }
}
