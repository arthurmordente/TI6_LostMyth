using System;
using UnityEngine;
using System.Threading.Tasks;
using Logic.Scripts.GameDomain.VisualFeedback;
using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using Logic.Scripts.GameDomain.MVC.Boss.Hocari;
using Logic.Scripts.GameDomain.MVC.Boss.Laki;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;

namespace Logic.Scripts.GameDomain.MVC.Boss {
    public class BossView : MonoBehaviour, IEffectable {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Collider _collider;
        [SerializeField] private Animator _animator;
        [SerializeField] private LakiBossAnimatorView _lakiAnimatorView;
        [SerializeField] private HocariBossAnimationBridge _hocariAnimationBridge;
        [SerializeField] private float _phaseTransitionDuration = 4.5f;

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
            if (LakiBossShieldRuntime.IsLakiShieldBlockingCombatInteraction()) return;
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
            if (UsesLakiAnimator()) return;
            if (_animator == null) return;
            _animator.SetBool("Moving", isMoving);
        }

        public void PlayPhaseTransition() {
            if (UsesLakiAnimator()) return;
            var hocari = ResolveHocariAnimationBridge();
            if (hocari != null && hocari.IsActive) {
                hocari.PlayPhaseTransition();
                return;
            }

            if (_animator == null) return;
            _animator.SetTrigger("PhaseTransition");
        }

        public void PlayAttackPrep(int attackId) {
            if (UsesLakiAnimator()) return;
            if (_animator == null) return;
            _animator.SetInteger("AttackId", attackId);
            _animator.SetTrigger("AttackPrep");
        }

        public void SetAttackLoop(bool looping) {
            if (UsesLakiAnimator()) return;
            if (_animator == null) return;
            _animator.SetBool("AttackLoop", looping);
        }

        public void PlayAttackFinish() {
            if (UsesLakiAnimator()) return;
            if (_animator == null) return;
            _animator.SetTrigger("AttackFinish");
            _animator.SetInteger("AttackId", -1);
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
				if (UsesLakiAnimator() && !inTransition && st.IsTag(LakiAnimatorParams.TagPerformanceLoop)) return;
				elapsed += Time.deltaTime;
				await Task.Yield();
			}
		}

		public async Task WaitUntilStateTagNormalizedAsync(string tag, float normalizedTime, float timeoutSeconds = 3f, int layer = 0)
		{
			if (UsesLakiAnimator() && string.Equals(tag, "AttackPrep", StringComparison.Ordinal))
				tag = LakiAnimatorParams.TagAbility;
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

		// Helper específico para gating do movimento: libera quando (MoveLoop ativo) OU (MovePrep >= threshold)
		public async Task WaitUntilMoveLoopOrPrepAsync(float prepThreshold = 0.9f, float timeoutSeconds = 3.5f, int layer = 0)
		{
			if (_animator == null) return;
			prepThreshold = Mathf.Clamp01(prepThreshold);
			float elapsed = 0f;
			while (elapsed < Mathf.Max(0.01f, timeoutSeconds))
			{
				bool inTransition = _animator.IsInTransition(layer);
				var st = _animator.GetCurrentAnimatorStateInfo(layer);
				if (!inTransition)
				{
					if (st.IsTag("MoveLoop")) return;
					if (st.IsTag("MovePrep") && st.normalizedTime >= prepThreshold) return;
				}
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

        public LineRenderer GetPointLineRenderer() => null;

        public GameObject GetReferenceTargetPrefab() {
            return gameObject;
        }

        public void SetSkillTargetingHighlight(bool active) {
            if (active && LakiBossShieldRuntime.IsLakiShieldBlockingCombatInteraction()) return;
            SkillTargetingHighlightBridge.SetHighlighted(this, active);
        }

        private bool UsesLakiAnimator()
        {
            if (ResolveLakiAnimatorView() != null) return true;
            if (_animator == null) return false;
            return AnimatorHasParameter(LakiAnimatorParams.PerformancePrep)
                && !AnimatorHasParameter("AttackPrep");
        }

        public void PlayLakiAttackImpact()
        {
            if (!UsesLakiAnimator()) return;
            ResolveLakiAnimatorView()?.PlayAbility();
        }

        private LakiBossAnimatorView ResolveLakiAnimatorView()
        {
            if (_lakiAnimatorView != null) return _lakiAnimatorView;
            _lakiAnimatorView = GetComponent<LakiBossAnimatorView>();
            if (_lakiAnimatorView == null)
                _lakiAnimatorView = GetComponentInChildren<LakiBossAnimatorView>(true);
            return _lakiAnimatorView;
        }

        private HocariBossAnimationBridge ResolveHocariAnimationBridge()
        {
            if (_hocariAnimationBridge != null) return _hocariAnimationBridge;
            _hocariAnimationBridge = GetComponent<HocariBossAnimationBridge>();
            if (_hocariAnimationBridge == null)
                _hocariAnimationBridge = GetComponentInChildren<HocariBossAnimationBridge>(true);
            return _hocariAnimationBridge;
        }

        private bool AnimatorHasParameter(string name)
        {
            if (_animator == null || string.IsNullOrEmpty(name)) return false;
            var parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name) return true;
            }
            return false;
        }

    }
}


