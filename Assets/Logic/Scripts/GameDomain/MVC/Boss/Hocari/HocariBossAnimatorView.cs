using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Boss.Hocari.Animation;

namespace Logic.Scripts.GameDomain.MVC.Boss.Hocari
{
    public class HocariBossAnimatorView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        public void SetAnimator(Animator animator) => _animator = animator;

        public void SetBossPhase(int phase)
        {
            if (_animator == null) return;
            _animator.SetInteger(HocariAnimatorParams.BossPhase, Mathf.Clamp(phase, 0, 1));
        }

        public void PlayPhaseTransition()
        {
            if (_animator == null || !HasParameter(HocariAnimatorParams.PhaseTransition)) return;
            _animator.SetTrigger(HocariAnimatorParams.PhaseTransition);
        }

        public void PlayHit()
        {
            if (_animator == null || !HasParameter(HocariAnimatorParams.Hit)) return;
            _animator.SetTrigger(HocariAnimatorParams.Hit);
        }

        public void PlayDeath()
        {
            if (_animator == null || !HasParameter(HocariAnimatorParams.Death)) return;
            _animator.SetTrigger(HocariAnimatorParams.Death);
        }

        bool HasParameter(string name)
        {
            if (_animator == null || string.IsNullOrEmpty(name)) return false;
            foreach (var p in _animator.parameters)
            {
                if (p.name == name) return true;
            }

            return false;
        }

        public bool UsesUnifiedController()
        {
            if (_animator == null) return false;
            foreach (var p in _animator.parameters)
            {
                if (p.name == HocariAnimatorParams.BossPhase) return true;
            }

            return false;
        }
    }
}
