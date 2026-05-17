using Logic.Scripts.GameDomain.Exploration;
using Logic.Scripts.Services.CommandFactory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Logic.Scripts.GameDomain.Exploration.QuitConfirmation
{
    public sealed class ExplorationQuitConfirmationService : IExplorationQuitConfirmationService, System.IDisposable
    {
        private readonly IExplorationQuitConfirmationView _view;
        private readonly ICommandFactory _commandFactory;
        private readonly GameObject _host;

        public ExplorationQuitConfirmationService(ICommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
            _host = new GameObject(nameof(ExplorationQuitConfirmationView));
            var viewComponent = _host.AddComponent<ExplorationQuitConfirmationView>();
            viewComponent.BindService(this);
            _view = viewComponent;
        }

        public bool IsVisible => _view.IsVisible;

        public void HandleEscapePressed()
        {
            if (!IsVisible)
            {
                Show();
                return;
            }

            QuitApplicationUtility.Quit();
        }

        public void ProcessDismissInput()
        {
            if (!IsVisible) return;
            if (WasNonEscapeDismissPressedThisFrame())
                Hide();
        }

        public void ForceHide()
        {
            if (IsVisible)
                Hide();
        }

        private void Show()
        {
            ExplorationModalInputGate.Push();
            ExplorationInteractInputGate.Push();
            _commandFactory.CreateCommandVoid<StopMoveInputCommand>().Execute();
            _view.Show();
        }

        private void Hide()
        {
            if (!IsVisible) return;
            _view.Hide();
            ExplorationInteractInputGate.Pop();
            ExplorationModalInputGate.Pop();
        }

        private static bool WasNonEscapeDismissPressedThisFrame()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                    return false;
                if (Keyboard.current.anyKey.wasPressedThisFrame)
                    return true;
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame
                    || Mouse.current.rightButton.wasPressedThisFrame
                    || Mouse.current.middleButton.wasPressedThisFrame)
                    return true;
            }

            var gamepad = Gamepad.current;
            if (gamepad == null) return false;

            return gamepad.buttonSouth.wasPressedThisFrame
                || gamepad.buttonEast.wasPressedThisFrame
                || gamepad.buttonWest.wasPressedThisFrame
                || gamepad.buttonNorth.wasPressedThisFrame
                || gamepad.leftShoulder.wasPressedThisFrame
                || gamepad.rightShoulder.wasPressedThisFrame
                || gamepad.leftTrigger.wasPressedThisFrame
                || gamepad.rightTrigger.wasPressedThisFrame
                || gamepad.dpad.up.wasPressedThisFrame
                || gamepad.dpad.down.wasPressedThisFrame
                || gamepad.dpad.left.wasPressedThisFrame
                || gamepad.dpad.right.wasPressedThisFrame;
        }

        public void Dispose()
        {
            ForceHide();
            if (_host != null)
                Object.Destroy(_host);
        }
    }
}
