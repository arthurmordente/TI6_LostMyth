using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills {
    [CreateAssetMenu(fileName = "SkillVisualCatalog", menuName = "Scriptable Objects/Skills/Skill Visual Catalog")]
    public class SkillVisualCatalogSO : ScriptableObject {
        [Serializable]
        public struct DivinityTypeVisual {
            public SkillDivinity Divinity;
            public SkillType SkillType;
            public Sprite BackgroundPaint;
            public Sprite Frame;
        }

        [SerializeField] private DivinityTypeVisual[] _entries = Array.Empty<DivinityTypeVisual>();

        [NonSerialized] private Dictionary<(SkillDivinity, SkillType), DivinityTypeVisual> _cache;

        public bool TryGet(SkillDivinity divinity, SkillType skillType,
            out Sprite backgroundPaint, out Sprite frame) {
            backgroundPaint = null;
            frame = null;
            if (_cache == null)
                BuildCache();
            if (!_cache.TryGetValue((divinity, skillType), out var entry))
                return false;
            backgroundPaint = entry.BackgroundPaint;
            frame = entry.Frame;
            return backgroundPaint != null || frame != null;
        }

        private void BuildCache() {
            _cache = new Dictionary<(SkillDivinity, SkillType), DivinityTypeVisual>();
            if (_entries == null) return;
            for (int i = 0; i < _entries.Length; i++) {
                var e = _entries[i];
                _cache[(e.Divinity, e.SkillType)] = e;
            }
        }

        private void OnValidate() {
            _cache = null;
        }
    }
}
