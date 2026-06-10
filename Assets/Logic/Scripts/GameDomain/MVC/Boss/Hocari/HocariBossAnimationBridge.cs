using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Boss.Hocari.Animation;

namespace Logic.Scripts.GameDomain.MVC.Boss.Hocari
{
    public class HocariBossAnimationBridge : MonoBehaviour
    {
        [SerializeField] private HocariBossAnimatorView _view;

        private void Awake()
        {
            if (_view == null)
                _view = GetComponent<HocariBossAnimatorView>();
            if (_view == null)
                _view = GetComponentInChildren<HocariBossAnimatorView>(true);
        }

        public bool IsActive => _view != null && _view.UsesUnifiedController();

        public void SetBossPhase(int phaseIndex) => _view?.SetBossPhase(phaseIndex);

        public void PlayPhaseTransition() => _view?.PlayPhaseTransition();

        public void PlayHit() => _view?.PlayHit();

        public void PlayDeath() => _view?.PlayDeath();
    }
}
