using System.Threading.Tasks;
using Logic.Scripts.Services.AudioService;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>
    /// Arena tile shuffle timed to <see cref="SfxIds.Laki_Turno"/> clip length.
    /// </summary>
    public static class LakiArenaRerollPresentation
    {
        const int ShuffleSteps = 16;
        const float FallbackShuffleDurationSeconds = 0.45f;
        const float ShortShuffleDurationSeconds = 1f;
        const int FullDurationRerollCount = 2;

        static int _rerollPresentationCount;

        public static void ResetRerollPresentationCount() => _rerollPresentationCount = 0;

        public static async Task RunShuffleWithTurnoSfxAsync(
            RouletteArenaService arena,
            IRouletteArenaVisual visual,
            IAudioService audio,
            int shuffleTurnSeed,
            int playerTileIndex,
            int finalRerollTurnNumber,
            System.Random finalRerollRng)
        {
            if (arena == null) return;

            _rerollPresentationCount++;
            bool useFullDuration = _rerollPresentationCount <= FullDurationRerollCount;

            float duration = useFullDuration ? FallbackShuffleDurationSeconds : ShortShuffleDurationSeconds;
            if (useFullDuration
                && audio != null
                && audio.TryGetSfxDuration(SfxIds.Laki_Turno, out float clipLength)
                && clipLength > 0f)
            {
                duration = clipLength;
            }

            audio?.PlaySfx(SfxIds.Laki_Turno, AudioChannelType.SfxBoss);

            float stepDelaySeconds = duration / ShuffleSteps;
            float elapsed = 0f;
            for (int i = 0; i < ShuffleSteps; i++)
            {
                arena.RandomizeVisualMapping(new System.Random((shuffleTurnSeed + i + 1) * 104729 + playerTileIndex));
                visual?.RefreshFrom(arena);
                int delayMs = Mathf.Max(1, Mathf.RoundToInt(stepDelaySeconds * 1000f));
                await Task.Delay(delayMs);
                elapsed += stepDelaySeconds;
            }

            float remainder = duration - elapsed;
            if (remainder > 0.01f)
                await Task.Delay(Mathf.RoundToInt(remainder * 1000f));

            arena.RerollTiles(finalRerollTurnNumber, finalRerollRng ?? new System.Random(finalRerollTurnNumber * 7919 + 17));
            visual?.RefreshFrom(arena);
        }
    }
}
