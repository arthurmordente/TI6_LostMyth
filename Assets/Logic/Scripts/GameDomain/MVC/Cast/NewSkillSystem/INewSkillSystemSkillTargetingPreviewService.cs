using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem {
    public interface INewSkillSystemSkillTargetingPreviewService {
        /// <param name="aimVisualRoot">
        /// Instantiated aim prefab root: <see cref="SkillDataSO.AreaAimPrefab"/> (Area),
        /// <see cref="SkillDataSO.ProjectileAimPrefab"/> (Projectile), or <see cref="SkillDataSO.SelfAimPrefab"/> (Self).
        /// </param>
        void Begin(SkillDataSO skill, IPlayableUnit playableCaster, Transform aimVisualRoot = null);
        void End();
    }
}
