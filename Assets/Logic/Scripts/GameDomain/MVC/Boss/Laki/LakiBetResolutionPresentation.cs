using System.Threading.Tasks;
using Logic.Scripts.GameDomain.MVC.Boss.Hocari;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using Logic.Scripts.Services.AudioService;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki
{
    public static class LakiBetResolutionPresentation
    {
        public const float BetReactionTimeoutSeconds = 5f;

        public static async Task PlayAsync(
            bool playerWon,
            bool isTie,
            int playerSum,
            int bossSum,
            BossView bossView,
            IAudioService audioService,
            bool finishThrowDieFirst)
        {
            if (finishThrowDieFirst && bossView != null)
            {
                var bridge = bossView.GetComponentInChildren<LakiBossAnimationBridge>(true);
                if (bridge != null)
                    await bridge.FinishDiceThrowSequenceAsync();
            }

            if (!isTie)
            {
                LakiArenaPresentationEvents.NotifyBetResolved(playerWon);
                TriggerBossBetReaction(bossView, playerWon, audioService);

                var lakiView = ResolveLakiAnimatorView(bossView);
                var erzaDriver = ResolveErzaAnimatorDriver();

                await Task.WhenAll(
                    WaitLakiBetReactionAsync(lakiView),
                    WaitErzaBetReactionAsync(erzaDriver));
            }

            DiceUiRuntime.RequestScoreboardCelebration(playerSum, bossSum, playerWon, isTie);
            await Task.Delay(650);
        }

        public static Task PlayDiceAttackAsync(in DiceAttackResult result, BossView bossView, IAudioService audioService) =>
            PlayAsync(
                result.PlayerWon,
                result.IsTie,
                result.PlayerSum,
                result.BossSum,
                bossView,
                audioService,
                finishThrowDieFirst: true);

        static void TriggerBossBetReaction(BossView bossView, bool playerWon, IAudioService audioService)
        {
            if (!BossViewUsesLakiAnimator(bossView))
            {
                bossView?.GetComponentInChildren<HocariBossAnimationBridge>(true)?.PlayHit();
                return;
            }

            string sfxId = playerWon ? SfxIds.Laki_Perdendo : SfxIds.Laki_Ganhando;
            if (!string.IsNullOrEmpty(sfxId))
                audioService?.PlaySfx(sfxId, AudioChannelType.SfxBoss);

            ResolveLakiAnimatorView(bossView)?.PlayBetReaction(!playerWon);
        }

        static async Task WaitLakiBetReactionAsync(LakiBossAnimatorView view)
        {
            if (view == null) return;
            await view.WaitUntilBetReactionCompleteAsync(BetReactionTimeoutSeconds);
        }

        static async Task WaitErzaBetReactionAsync(ErzahlerPlayerAnimatorDriver driver)
        {
            if (driver == null) return;
            await driver.WaitUntilBetReactionCompleteAsync(BetReactionTimeoutSeconds);
        }

        static bool BossViewUsesLakiAnimator(BossView bossView)
        {
            if (bossView == null) return false;
            return bossView.GetComponentInChildren<LakiBossAnimationBridge>(true) != null
                || bossView.GetComponentInChildren<LakiBossAnimatorBootstrap>(true) != null;
        }

        static LakiBossAnimatorView ResolveLakiAnimatorView(BossView bossView) =>
            bossView != null ? bossView.GetComponentInChildren<LakiBossAnimatorView>(true) : null;

        static ErzahlerPlayerAnimatorDriver ResolveErzaAnimatorDriver()
        {
            var nara = Object.FindFirstObjectByType<NaraView>();
            return nara != null ? nara.ErzahlerAnimatorDriver : null;
        }
    }
}
