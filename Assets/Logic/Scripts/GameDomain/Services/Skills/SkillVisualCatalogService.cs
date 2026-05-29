using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills {
    public sealed class SkillVisualCatalogService : ISkillVisualCatalog {
        private readonly SkillVisualCatalogSO _catalog;

        public SkillVisualCatalogService(SkillVisualCatalogSO catalog) {
            _catalog = catalog;
        }

        public bool TryGetLayerSprites(SkillDivinity divinity, SkillType skillType,
            out Sprite backgroundPaint, out Sprite frame) {
            if (_catalog == null) {
                backgroundPaint = null;
                frame = null;
                return false;
            }
            return _catalog.TryGet(divinity, skillType, out backgroundPaint, out frame);
        }
    }
}
