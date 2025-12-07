using System;
using UnityEngine;
using System.Threading.Tasks;

namespace Logic.Scripts.GameDomain.MVC.Boss {
    public class BossView : MonoBehaviour, IEffectable {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Collider _collider;
        [SerializeField] private Animator _animator;
        [SerializeField] private float _phaseTransitionDuration = 1.0f;

        private Action<int> _onPreviewHeal;
        private Action<int> _onPreviewDamage;
        private Action<int> _onTakeDamage;
        private Action<int> _onHeal;

        public void SetupCallbacks(Action<int> onPreviewHeal, Action<int> onPreviewDamage,
            Action<int> onTakeDamage, Action<int> onHeal) {
            _onPreviewHeal = onPreviewHeal;
            _onPreviewDamage = onPreviewDamage;
            _onTakeDamage = onTakeDamage;
            _onHeal = onHeal;
        }

        public void RemoveAllCallbacks() {
        }

        public Rigidbody GetRigidbody() {
            return _rigidbody;
        }

        public Transform GetReferenceTransform() {
            return transform;
        }

        public void PreviewHeal(int healAmound) {
            _onPreviewHeal?.Invoke(healAmound);
        }

        public void PreviewDamage(int damageAmound) {
            _onPreviewDamage?.Invoke(damageAmound);
        }

        public void ResetPreview() {
            throw new NotImplementedException();
        }

        public void TakeDamage(int damageAmount) {
            Debug.Log("Take damage bossView");
            _onTakeDamage?.Invoke(damageAmount);
        }

        public void TakeDamagePerTurn(int damageAmount, int duration) {
            throw new NotImplementedException();
        }

        public void Heal(int healAmount) {
            _onHeal?.Invoke(healAmount);
        }

        public void HealPerTurn(int healAmount, int duration) {
            throw new NotImplementedException();
        }

        public void SetMoving(bool isMoving) {
            if (_animator != null) {
                _animator.SetBool("Moving", isMoving);
            }
        }

        public void PlayPhaseTransition() {
            if (_animator != null) {
                _animator.SetTrigger("PhaseTransition");
            }
        }

        public void PlayAttackPrep(int attackId) {
            if (_animator == null) return;
            _animator.SetInteger("AttackId", attackId);
            _animator.SetTrigger("AttackPrep");
        }

        public void SetAttackLoop(bool looping) {
            if (_animator == null) return;
            _animator.SetBool("AttackLoop", looping);
        }

        public void PlayAttackFinish() {
            if (_animator == null) return;
            _animator.SetTrigger("AttackFinish");
        }

        public void PlayMovePrep() {
            if (_animator == null) return;
            _animator.SetTrigger("MovePrep");
        }

        public void PlayMoveFinish() {
            if (_animator == null) return;
            _animator.SetTrigger("MoveFinish");
        }

        public void PlayIdle() {
            if (_animator == null) return;
            _animator.SetTrigger("Idle");
        }

        public async Task WaitUntilAttackLoopAsync(float timeoutSeconds = 3f, int layer = 0) {
            if (_animator == null) return;
            float elapsed = 0f;
            while (elapsed < Mathf.Max(0.01f, timeoutSeconds)) {
                // Avoid reporting loop reached while in transition
                bool inTransition = _animator.IsInTransition(layer);
                var st = _animator.GetCurrentAnimatorStateInfo(layer);
                if (!inTransition && st.IsTag("AttackLoop")) return;
                elapsed += Time.deltaTime;
                await Task.Yield();
            }
        }

		public async Task WaitUntilIdleAsync(float timeoutSeconds = 3f, int layer = 0) {
			if (_animator == null) return;
			float elapsed = 0f;
			while (elapsed < Mathf.Max(0.01f, timeoutSeconds)) {
				bool inTransition = _animator.IsInTransition(layer);
				var st = _animator.GetCurrentAnimatorStateInfo(layer);
				if (!inTransition && st.IsTag("Idle")) return;
				elapsed += Time.deltaTime;
				await Task.Yield();
			}
		}

		public async Task WaitUntilStateTagNormalizedAsync(string tag, float normalizedTime, float timeoutSeconds = 3f, int layer = 0)
		{
			if (_animator == null) return;
			normalizedTime = Mathf.Clamp01(normalizedTime);
			float elapsed = 0f;
			while (elapsed < Mathf.Max(0.01f, timeoutSeconds))
			{
				bool inTransition = _animator.IsInTransition(layer);
				var st = _animator.GetCurrentAnimatorStateInfo(layer);
				if (!inTransition && st.IsTag(tag) && st.normalizedTime >= normalizedTime) return;
				elapsed += Time.deltaTime;
				await Task.Yield();
			}
		}

        public float GetPhaseTransitionDuration() {
            return _phaseTransitionDuration;
        }

        public Transform GetTransformCastPoint() {
            return transform;
        }

        public GameObject GetReferenceTargetPrefab() {
            return gameObject;
        }
    }
}


