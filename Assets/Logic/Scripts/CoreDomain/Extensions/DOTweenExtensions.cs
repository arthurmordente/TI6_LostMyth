using System.Threading;
using DG.Tweening;
using Logic.Scripts.Utils;

namespace Logic.Scripts.Extensions
{
    public static class DOTweenExtensions
    {
        public static async Awaitable WithCancellationSafe(this Tween tween, CancellationToken cancellationToken)
        {
            KillTweenImmediatelyWhenTokenIsCanceled(tween, cancellationToken);
            await WaitUntilCompleted(tween, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static async Awaitable WaitUntilCompleted(Tween tween, CancellationToken cancellationToken)
        {
            await AwaitableUtils.WaitUntil(() => !tween.active || tween.IsComplete(), cancellationToken);
        }

        private static void KillTweenImmediatelyWhenTokenIsCanceled(Tween tween, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                if (tween != null && tween.IsActive())
                    tween.Kill();
            });
        }
    }
}
