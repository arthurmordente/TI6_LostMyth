using Logic.Scripts.GameDomain.MVC.Shared;

namespace Logic.Scripts.GameDomain.MVC.Cast.Paschoal {
    public interface IPaschoalSkillTargetingPreviewService {
        void Begin(SkillDataSO skill, IPlayableUnit playableCaster);
        void End();
    }
}
