using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills {
    /// <summary>Fallback when <see cref="SkillVisualCatalogSO"/> is not assigned on GameInstaller.</summary>
    public sealed class NullSkillVisualCatalog : ISkillVisualCatalog {
        public static readonly NullSkillVisualCatalog Instance = new();

        public bool TryGetLayerSprites(SkillDivinity divinity, SkillType skillType,
            out Sprite backgroundPaint, out Sprite frame) {
            backgroundPaint = null;
            frame = null;
            return false;
        }
    }
}
