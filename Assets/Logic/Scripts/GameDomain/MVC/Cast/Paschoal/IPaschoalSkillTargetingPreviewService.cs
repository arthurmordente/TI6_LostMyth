using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Cast.Paschoal {
    public interface IPaschoalSkillTargetingPreviewService {
        /// <param name="aoeVisualRoot">Optional instantiated <see cref="SkillDataSO.AoEPrefab"/> root; follows aim and scales to match <see cref="SkillDataSO.AreaOfEffect"/>.</param>
        void Begin(SkillDataSO skill, IPlayableUnit playableCaster, Transform aoeVisualRoot = null);
        void End();
    }
}
