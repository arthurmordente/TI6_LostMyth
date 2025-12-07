using System.Threading;
using UnityEngine;

namespace Logic.Scripts.GameDomain.GameInputActions {
    public interface IGameInputActionsController {
        void EnableGameplayInputs();
        void EnableExplorationInputs();
        void DisableGameplayInputs();
        void DisableUIInputs();
        void DisableExplorationInputs();
        void RegisterUIGameplayInputListeners();
        void UnregisterUIGameplayInputListeners();
        void RegisterGameplayInputListeners();
        void UnregisterGameplayInputListeners();
        void RegisterExplorationInputListeners();
        void UnregisterExplorationInputListeners();
        Awaitable WaitForAnyKeyPressed(CancellationTokenSource cancellationTokenSource, bool canPressOverGui);
    }
}
