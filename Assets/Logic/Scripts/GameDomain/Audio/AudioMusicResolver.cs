using System;
using Logic.Scripts.GameDomain.MVC.Boss;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.Logger.Base;

namespace Logic.Scripts.GameDomain.Audio {
    public static class AudioMusicResolver {
        public static string ResolveFightMusic(BossConfigurationSO bossConfiguration) {
            if (bossConfiguration == null)
                return MusicIds.FightLaki;

            return ResolveFightMusic(bossConfiguration.BossDisplayName);
        }

        public static string ResolveFightMusic(string bossDisplayName) {
            if (string.IsNullOrWhiteSpace(bossDisplayName))
                return MusicIds.FightLaki;

            var normalized = bossDisplayName.Trim();
            if (normalized.Equals("Hokari", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Hocari", StringComparison.OrdinalIgnoreCase))
                return MusicIds.FightHokari;

            if (normalized.Equals("Laki", StringComparison.OrdinalIgnoreCase))
                return MusicIds.FightLaki;

            LogService.LogError($"[Audio] Unknown boss for fight music: '{bossDisplayName}', defaulting to FightLaki.");
            return MusicIds.FightLaki;
        }

        public static string ResolveFightMusic(LevelTurnData levelTurnData) {
            if (levelTurnData == null)
                return MusicIds.FightLaki;

            if (levelTurnData.BossConfiguration != null)
                return ResolveFightMusic(levelTurnData.BossConfiguration);

            return ResolveFightMusic(levelTurnData.GetEffectiveBossHudDisplayName());
        }
    }
}
