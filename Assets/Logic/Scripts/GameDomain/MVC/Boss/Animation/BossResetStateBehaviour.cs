using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Animation
{
	// Attach this to the ResetState inside the Hocari Attacks Sub-StateMachine.
	// On enter, it clears transient animator parameters to guarantee a clean exit to Idle.
	// This avoids stale loop/trigger flags leaking into the next turn.
	public sealed class BossResetStateBehaviour : StateMachineBehaviour
	{
		[Header("Parameter Names")]
		[SerializeField] private string _attackIdParam = "AttackId";
		[SerializeField] private string _attackLoopParam = "AttackLoop";
		[SerializeField] private string _movingParam = "Moving";

		[SerializeField] private string _attackPrepTrigger = "AttackPrep";
		[SerializeField] private string _attackFinishTrigger = "AttackFinish";
		[SerializeField] private string _movePrepTrigger = "MovePrep";
		[SerializeField] private string _moveFinishTrigger = "MoveFinish";
		[SerializeField] private string _idleTrigger = "Idle";

		[Header("Reset Behaviour")]
		[SerializeField] private int _attackIdResetValue = -1;
		[SerializeField] private bool _resetAttackId = true;
		[SerializeField] private bool _resetAttackLoop = true;
		[SerializeField] private bool _resetMoving = true;

		[SerializeField] private bool _resetAttackPrepTrigger = true;
		[SerializeField] private bool _resetAttackFinishTrigger = true;
		[SerializeField] private bool _resetMovePrepTrigger = true;
		[SerializeField] private bool _resetMoveFinishTrigger = true;

		// If true, forces Idle after clearing flags. This should route back to Idle from the sub-state.
		[SerializeField] private bool _forceIdle = true;

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			// Reset numeric/bool state
			if (_resetAttackId && !string.IsNullOrEmpty(_attackIdParam))
				animator.SetInteger(_attackIdParam, _attackIdResetValue);
			if (_resetAttackLoop && !string.IsNullOrEmpty(_attackLoopParam))
				animator.SetBool(_attackLoopParam, false);
			if (_resetMoving && !string.IsNullOrEmpty(_movingParam))
				animator.SetBool(_movingParam, false);

			// Clear transient triggers from both movement and attack flows
			if (_resetAttackPrepTrigger && !string.IsNullOrEmpty(_attackPrepTrigger))
				animator.ResetTrigger(_attackPrepTrigger);
			if (_resetAttackFinishTrigger && !string.IsNullOrEmpty(_attackFinishTrigger))
				animator.ResetTrigger(_attackFinishTrigger);
			if (_resetMovePrepTrigger && !string.IsNullOrEmpty(_movePrepTrigger))
				animator.ResetTrigger(_movePrepTrigger);
			if (_resetMoveFinishTrigger && !string.IsNullOrEmpty(_moveFinishTrigger))
				animator.ResetTrigger(_moveFinishTrigger);

			// Nudge the machine back to Idle at the upper level
			if (_forceIdle && !string.IsNullOrEmpty(_idleTrigger))
				animator.SetTrigger(_idleTrigger);
		}
	}
}


