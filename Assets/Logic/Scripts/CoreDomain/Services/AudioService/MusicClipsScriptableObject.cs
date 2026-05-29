using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.Services.AudioService {
    [Serializable]
    public struct MusicClipEntry {
        public string Id;
        public AudioClip Clip;
    }

    [CreateAssetMenu(fileName = "GameplayMusicClips", menuName = "Scriptable Objects/Audio/Music Clips")]
    public class MusicClipsScriptableObject : ScriptableObject {
        [SerializeField] private List<MusicClipEntry> _entries = new();

        [NonSerialized] private Dictionary<string, AudioClip> _cache;

        public bool TryGetClip(string id, out AudioClip clip) {
            if (_cache == null)
                BuildCache();
            if (string.IsNullOrEmpty(id)) {
                clip = null;
                return false;
            }
            return _cache.TryGetValue(id, out clip);
        }

        private void BuildCache() {
            _cache = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            if (_entries == null) return;
            for (int i = 0; i < _entries.Count; i++) {
                var e = _entries[i];
                if (e.Clip == null || string.IsNullOrWhiteSpace(e.Id)) continue;
                _cache[e.Id.Trim()] = e.Clip;
            }
        }

        private void OnValidate() {
            _cache = null;
        }
    }

    public static class MusicIds {
        public const string Menu = "Menu";
        public const string FightHokari = "FightHokari";
        public const string FightLaki = "FightLaki";
        /// <summary>Alias for <see cref="FightLaki"/> (clip: Laki_song.mp3).</summary>
        public const string Laki_Song = FightLaki;
    }
}
