using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Hocari.Animation
{
    public sealed class HocariPhaseTransitionBehaviour : StateMachineBehaviour
    {
        [SerializeField] private string _bossPhaseParam = HocariAnimatorParams.BossPhase;
        [SerializeField] private int _targetPhase = HocariAnimatorParams.PhaseTwo;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!string.IsNullOrEmpty(_bossPhaseParam))
                animator.SetInteger(_bossPhaseParam, _targetPhase);
        }
    }
}
