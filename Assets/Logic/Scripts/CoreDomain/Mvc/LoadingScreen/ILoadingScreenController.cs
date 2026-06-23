using System.Threading;
using UnityEngine;

namespace Logic.Scripts.Core.Mvc.LoadingScreen
{
    public interface ILoadingScreenController
    {
        void SetupLoadingView(LoadingTipPoolSO tipPool);
        void ShowTransitionTip();
        void EnableContinuePrompt();
        Awaitable WaitForPlayerContinue(CancellationTokenSource cancellationTokenSource);
        void Hide();
    }
}
