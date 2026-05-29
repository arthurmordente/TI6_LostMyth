using System.Threading;
using UnityEngine;

namespace Logic.Scripts.Services.AudioService {
    public interface IAudioService {
        void InitEntryPoint();
        void AddMusicClips(MusicClipsScriptableObject pack);
        void AddSfxClips(SfxClipsScriptableObject pack);
        void PlayMusic(string musicId);
        void StopMusic();
        void PlaySfx(string sfxId, AudioChannelType channel, AudioPlayType playType = AudioPlayType.OneShot);
        bool HasSfx(string sfxId);
        bool TryPlaySfx(string sfxId, AudioChannelType channel, AudioPlayType playType = AudioPlayType.OneShot);
        bool TryGetSfxDuration(string sfxId, out float durationSeconds);
        void SetSegmentLoopingSfx(string sfxId, AudioChannelType channel, float segmentLengthSeconds, bool play);
        void StopLoopingSfx(AudioChannelType channel);
        Awaitable PlaySfxAsync(string sfxId, AudioChannelType channel, CancellationTokenSource cancellationTokenSource,
            AudioPlayType playType = AudioPlayType.OneShot);
        void StopAllAudio();
    }
}
