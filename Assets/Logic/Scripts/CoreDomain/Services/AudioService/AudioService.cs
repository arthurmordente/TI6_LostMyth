using System.Collections.Generic;

using System.Threading;

using Logic.Scripts.Services.Logger.Base;

using UnityEngine;

using UnityEngine.Serialization;



namespace Logic.Scripts.Services.AudioService {

    public class AudioService : MonoBehaviour, IAudioService {

        [FormerlySerializedAs("_MusicAudioSource")]

        [SerializeField] private AudioSource _musicAudioSource;

        [FormerlySerializedAs("_FxAudioSource")]

        [SerializeField] private AudioSource _sfxCombatAudioSource;

        [SerializeField] private AudioSource _sfxUiAudioSource;

        [SerializeField] private AudioSource _sfxBossAudioSource;

        [SerializeField] private AudioSource _sfxAmbienceAudioSource;



        private readonly List<MusicClipsScriptableObject> _musicPacks = new();

        private readonly List<SfxClipsScriptableObject> _sfxPacks = new();

        private readonly Dictionary<AudioChannelType, AudioSource> _audioSourceByChannel = new();



        private AudioChannelType? _segmentLoopChannel;

        private float _segmentLoopLengthSeconds;



        public void InitEntryPoint() {

            ValidateChannelConfiguration();

            ResolveMissingSources();

            _audioSourceByChannel.Clear();

            RegisterChannel(AudioChannelType.Music, _musicAudioSource);

            RegisterChannel(AudioChannelType.SfxUi, _sfxUiAudioSource);

            RegisterChannel(AudioChannelType.SfxCombat, _sfxCombatAudioSource);

            RegisterChannel(AudioChannelType.SfxBoss, _sfxBossAudioSource);

            RegisterChannel(AudioChannelType.SfxAmbience, _sfxAmbienceAudioSource);

        }



        private void ValidateChannelConfiguration() {

            if (_musicAudioSource == null)

                LogService.LogError("[Audio] Music AudioSource not assigned on AudioService.");

            if (_sfxCombatAudioSource == null)

                LogService.LogError("[Audio] SfxCombat AudioSource not assigned on AudioService.");

            if (_sfxUiAudioSource == null)

                LogService.LogWarning("[Audio] SfxUi AudioSource not assigned — will fall back to SfxCombat.");

            if (_sfxBossAudioSource == null)

                LogService.LogWarning("[Audio] SfxBoss AudioSource not assigned — will fall back to SfxCombat.");

            if (_sfxAmbienceAudioSource == null)

                LogService.LogWarning("[Audio] SfxAmbience AudioSource not assigned — will fall back to SfxCombat.");



            if (_sfxUiAudioSource != null && _sfxCombatAudioSource != null

                && _sfxUiAudioSource == _sfxCombatAudioSource

                && (_sfxBossAudioSource == null || _sfxBossAudioSource == _sfxCombatAudioSource))

                LogService.LogWarning("[Audio] SfxUi and SfxBoss share SfxCombat AudioSource — UI/boss SFX may overlap.");

        }



        private void Update() {

            if (_segmentLoopChannel == null)

                return;

            if (!_audioSourceByChannel.TryGetValue(_segmentLoopChannel.Value, out var source)

                || source == null

                || !source.isPlaying

                || source.clip == null)

                return;



            if (source.time >= _segmentLoopLengthSeconds)

                source.time = 0f;

        }



        private void ResolveMissingSources() {

            if (_sfxUiAudioSource == null)

                _sfxUiAudioSource = _sfxCombatAudioSource;

            if (_sfxBossAudioSource == null)

                _sfxBossAudioSource = _sfxCombatAudioSource;

            if (_sfxAmbienceAudioSource == null)

                _sfxAmbienceAudioSource = _sfxCombatAudioSource;

        }



        private void RegisterChannel(AudioChannelType channel, AudioSource source) {

            if (source == null) {

                LogService.LogError($"[Audio] Missing AudioSource for channel {channel}");

                return;

            }

            _audioSourceByChannel[channel] = source;

        }



        public void AddMusicClips(MusicClipsScriptableObject pack) {

            if (pack != null && !_musicPacks.Contains(pack))

                _musicPacks.Add(pack);

        }



        public void AddSfxClips(SfxClipsScriptableObject pack) {

            if (pack != null && !_sfxPacks.Contains(pack))

                _sfxPacks.Add(pack);

        }



        public void PlayMusic(string musicId) {

            if (!TryGetMusicClip(musicId, out var clip))

                return;



            if (!_audioSourceByChannel.TryGetValue(AudioChannelType.Music, out var source) || source == null) {

                LogService.LogError("[Audio] Music channel not configured.");

                return;

            }



            if (source.mute || !source.enabled)

                return;



            source.Stop();

            source.clip = clip;

            source.loop = true;

            source.Play();

            LogService.LogTopic($"PlayMusic {musicId}", LogTopicType.Audio);

        }



        public void StopMusic() {

            if (_audioSourceByChannel.TryGetValue(AudioChannelType.Music, out var source) && source != null)

                source.Stop();

        }



        public void PlaySfx(string sfxId, AudioChannelType channel, AudioPlayType playType = AudioPlayType.OneShot) {

            TryPlaySfxClip(sfxId, channel, playType, out _);

        }



        public bool HasSfx(string sfxId) {

            if (string.IsNullOrEmpty(sfxId)) return false;

            foreach (var pack in _sfxPacks) {

                if (pack != null && pack.TryGetClip(sfxId, out _))

                    return true;

            }

            return false;

        }



        public bool TryPlaySfx(string sfxId, AudioChannelType channel, AudioPlayType playType = AudioPlayType.OneShot) {

            if (!HasSfx(sfxId)) return false;

            return TryPlaySfxClip(sfxId, channel, playType, out _);

        }



        public bool TryGetSfxDuration(string sfxId, out float durationSeconds) {

            if (TryGetSfxClip(sfxId, out var clip) && clip != null) {

                durationSeconds = clip.length;

                return true;

            }

            durationSeconds = 0f;

            return false;

        }



        public void SetSegmentLoopingSfx(string sfxId, AudioChannelType channel, float segmentLengthSeconds, bool play) {

            if (!play) {

                StopLoopingSfx(channel);

                return;

            }



            if (!TryGetSfxClip(sfxId, out var clip))

                return;



            var audioSource = ResolveSfxSource(channel);

            if (audioSource == null || audioSource.mute || !audioSource.enabled)

                return;



            float segment = Mathf.Clamp(segmentLengthSeconds, 0.05f, clip.length);



            audioSource.Stop();

            audioSource.clip = clip;

            audioSource.loop = false;

            audioSource.time = 0f;

            audioSource.Play();



            _segmentLoopChannel = channel;

            _segmentLoopLengthSeconds = segment;

            LogService.LogTopic($"PlaySfx {sfxId} segment-loop {segment:F2}s on {channel}", LogTopicType.Audio);

        }



        public void StopLoopingSfx(AudioChannelType channel) {

            if (_segmentLoopChannel == channel)

                _segmentLoopChannel = null;



            if (!_audioSourceByChannel.TryGetValue(channel, out var source) || source == null)

                return;



            source.Stop();

            source.loop = false;

            source.clip = null;

        }



        public async Awaitable PlaySfxAsync(string sfxId, AudioChannelType channel, CancellationTokenSource cancellationTokenSource,

            AudioPlayType playType = AudioPlayType.OneShot) {

            if (TryPlaySfxClip(sfxId, channel, playType, out var clip))

                await Awaitable.WaitForSecondsAsync(clip.length, cancellationTokenSource.Token);

        }



        private bool TryPlaySfxClip(string sfxId, AudioChannelType channel, AudioPlayType playType, out AudioClip clip) {

            clip = null;

            if (!TryGetSfxClip(sfxId, out clip))

                return false;



            var audioSource = ResolveSfxSource(channel);

            if (audioSource == null)

                return false;



            if (audioSource.mute || !audioSource.enabled)

                return false;



            switch (playType) {

                case AudioPlayType.OneShot:

                    audioSource.loop = false;

                    audioSource.PlayOneShot(clip);

                    break;

                case AudioPlayType.Loop:

                    audioSource.clip = clip;

                    audioSource.loop = true;

                    audioSource.Play();

                    break;

            }



            LogService.LogTopic($"PlaySfx {sfxId} on {channel}", LogTopicType.Audio);

            return true;

        }



        private bool TryGetMusicClip(string id, out AudioClip clip) {

            foreach (var pack in _musicPacks) {

                if (pack.TryGetClip(id, out clip))

                    return true;

            }

            LogService.LogError($"[Audio] No music clip for '{id}'");

            clip = null;

            return false;

        }



        private AudioSource ResolveSfxSource(AudioChannelType channel) {

            if (!_audioSourceByChannel.TryGetValue(channel, out var audioSource) || audioSource == null) {

                LogService.LogError($"[Audio] No channel {channel} found.");

                return null;

            }

            return audioSource;

        }



        private bool TryGetSfxClip(string id, out AudioClip clip) {

            foreach (var pack in _sfxPacks) {

                if (pack.TryGetClip(id, out clip))

                    return true;

            }

            LogService.LogError($"[Audio] No sfx clip for '{id}'");

            clip = null;

            return false;

        }



        public void StopAllAudio() {

            LogService.LogTopic("Stop all audio", LogTopicType.Audio);

            _segmentLoopChannel = null;

            foreach (var pair in _audioSourceByChannel) {

                var source = pair.Value;

                if (source == null) continue;

                source.Stop();

                source.loop = false;

                source.clip = null;

            }

        }

    }

}


