namespace Logic.Scripts.Services.AudioService {
    /// <summary>SFX da pasta <c>SFX's/Gerais</c> — valida o catálogo antes de tocar.</summary>
    public static class GeneralSfxFeedback {
        public static bool TryPlay(IAudioService audio, string sfxId, AudioChannelType channel) =>
            audio != null && audio.TryPlaySfx(sfxId, channel);

        public static void PlayMenuClick(IAudioService audio, bool secondary = false) =>
            TryPlay(audio, secondary ? SfxIds.UI_Clique2 : SfxIds.UI_Clique, AudioChannelType.SfxUi);

        public static void PlayGameOverStinger(IAudioService audio, bool isWin) =>
            TryPlay(audio, isWin ? SfxIds.UI_Tela_Vitoria : SfxIds.UI_Tela_Derrota, AudioChannelType.SfxUi);

        public static void PlayPortal(IAudioService audio) =>
            TryPlay(audio, SfxIds.UI_Portal, AudioChannelType.SfxUi);

        public static void PlayNpcTalking(IAudioService audio) =>
            TryPlay(audio, SfxIds.NPC_Falando, AudioChannelType.SfxUi);

        public static void PlayNewTurn(IAudioService audio) =>
            TryPlay(audio, SfxIds.UI_Novo_Turno, AudioChannelType.SfxUi);

        public static void PlayDiceRoll(IAudioService audio) =>
            TryPlay(audio, SfxIds.UI_Dados, AudioChannelType.SfxCombat);
    }
}
