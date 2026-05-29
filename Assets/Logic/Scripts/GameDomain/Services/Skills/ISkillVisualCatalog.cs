using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills {
    public interface ISkillVisualCatalog {
        bool TryGetLayerSprites(SkillDivinity divinity, SkillType skillType,
            out Sprite backgroundPaint, out Sprite frame);
    }
}
