using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem {
    public interface INewSkillSystemSkillTargetingPreviewService {
        /// <param name="aoeVisualRoot">
        /// Optional root for aim visuals: <see cref="SkillDataSO.AoEPrefab"/> instance for <see cref="Logic.Scripts.GameDomain.Services.Skills.SkillCastMode.Area"/>,
        /// or <see cref="SkillDataSO.ProjectileAimPreviewPrefab"/> for <see cref="Logic.Scripts.GameDomain.Services.Skills.SkillCastMode.Projectile"/>.
        /// Updated each frame to match aim position, facing, and length/radius.
        /// </param>
        void Begin(SkillDataSO skill, IPlayableUnit playableCaster, Transform aoeVisualRoot = null);
        void End();
    }
}
