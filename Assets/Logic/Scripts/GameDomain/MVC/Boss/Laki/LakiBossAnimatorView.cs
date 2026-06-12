using System.Threading.Tasks;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki
{
    public class LakiBossAnimatorView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        public void SetAnimator(Animator animator) => _animator = animator;

        /// <summary>Sorteio de performance: limpa triggers, define PerformanceId, entra em Prep (loop depois).</summary>
        public void BeginPerformanceTurn(int performanceId)
        {
            if (_animator == null) return;
            ResetCombatTriggers();
            _animator.SetBool(LakiAnimatorParams.PerformanceLoop, false);
            _animator.SetInteger(LakiAnimatorParams.PerformanceId, performanceId);
            _animator.SetTrigger(LakiAnimatorParams.PerformancePrep);
        }

        public void SetPerformanceLoop(bool looping)
        {
            if (_animator == null) return;
            _animator.SetBool(LakiAnimatorParams.PerformanceLoop, looping);
        }

        public void PlayPerformanceFinish()
        {
            if (_animator == null) return;
            _animator.SetBool(LakiAnimatorParams.PerformanceLoop, false);
            _animator.ResetTrigger(LakiAnimatorParams.PerformancePrep);
            _animator.ResetTrigger(LakiAnimatorParams.Ability);
            _animator.SetTrigger(LakiAnimatorParams.PerformanceFinish);
        }

        public void PlayAbility()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(LakiAnimatorParams.PerformancePrep);
            _animator.ResetTrigger(LakiAnimatorParams.PerformanceFinish);
            _animator.SetTrigger(LakiAnimatorParams.Ability);
        }

        public void PlaySpotlight()
        {
            if (_animator == null) return;
            _animator.SetTrigger(LakiAnimatorParams.Spotlight);
        }

        public void PlayHitReaction()
        {
            if (_animator == null || !HasParameter(LakiAnimatorParams.HitReaction)) return;
            _animator.SetTrigger(LakiAnimatorParams.HitReaction);
        }

        public void PlayBetReaction(bool bossWonBet)
        {
            if (_animator == null) return;
            string param = bossWonBet ? LakiAnimatorParams.BetWon : LakiAnimatorParams.BetLost;
            if (!HasParameter(param)) return;
            _animator.SetTrigger(param);
        }

        public void PlayDeath()
        {
            if (_animator == null || !HasParameter(LakiAnimatorParams.Death)) return;
            _animator.SetTrigger(LakiAnimatorParams.Death);
        }

        public void BeginThrowDie()
        {
            if (_animator == null || !HasParameter(LakiAnimatorParams.ThrowDiePrep)) return;
            _animator.SetBool(LakiAnimatorParams.ThrowDieLoop, false);
            _animator.SetTrigger(LakiAnimatorParams.ThrowDiePrep);
        }

        public void SetThrowDieLoop(bool looping)
        {
            if (_animator == null || !HasParameter(LakiAnimatorParams.ThrowDieLoop)) return;
            _animator.SetBool(LakiAnimatorParams.ThrowDieLoop, looping);
        }

        public void FinishThrowDie()
        {
            if (_animator == null || !HasParameter(LakiAnimatorParams.ThrowDieFinish)) return;
            _animator.SetBool(LakiAnimatorParams.ThrowDieLoop, false);
            _animator.SetTrigger(LakiAnimatorParams.ThrowDieFinish);
        }

        bool HasParameter(string name)
        {
            if (_animator == null || string.IsNullOrEmpty(name)) return false;
            var parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name) return true;
            }

            return false;
        }

        public void ResetCombatTriggers()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(LakiAnimatorParams.PerformancePrep);
            _animator.ResetTrigger(LakiAnimatorParams.PerformanceFinish);
            _animator.ResetTrigger(LakiAnimatorParams.Ability);
            _animator.ResetTrigger(LakiAnimatorParams.Spotlight);
        }

        public async Task WaitUntilStateTagAsync(string tag, float timeoutSeconds = 3f, int layer = 0)
        {
            if (_animator == null) return;
            float elapsed = 0f;
            while (elapsed < Mathf.Max(0.01f, timeoutSeconds))
            {
                if (!_animator.IsInTransition(layer))
                {
                    var st = _animator.GetCurrentAnimatorStateInfo(layer);
                    if (st.IsTag(tag)) return;
                }

                elapsed += Time.deltaTime;
                await Task.Yield();
            }
        }

        public async Task WaitUntilLeftStateTagAsync(string tag, float timeoutSeconds = 3f, int layer = 0)
        {
            if (_animator == null) return;
            float elapsed = 0f;
            while (elapsed < Mathf.Max(0.01f, timeoutSeconds))
            {
                if (!_animator.IsInTransition(layer))
                {
                    var st = _animator.GetCurrentAnimatorStateInfo(layer);
                    if (!st.IsTag(tag)) return;
                }

                elapsed += Time.deltaTime;
                await Task.Yield();
            }
        }

        public bool IsInPerformanceLoop(int layer = 0)
        {
            if (_animator == null) return false;
            if (_animator.IsInTransition(layer)) return false;
            return _animator.GetCurrentAnimatorStateInfo(layer).IsTag(LakiAnimatorParams.TagPerformanceLoop);
        }

        public async Task WaitUntilBetReactionCompleteAsync(float timeoutSeconds = 5f, int layer = 0)
        {
            if (_animator == null) return;
            if (!HasParameter(LakiAnimatorParams.BetWon) && !HasParameter(LakiAnimatorParams.BetLost))
                return;

            float elapsed = 0f;
            bool enteredBet = false;
            while (elapsed < Mathf.Max(0.01f, timeoutSeconds))
            {
                if (!_animator.IsInTransition(layer))
                {
                    var st = _animator.GetCurrentAnimatorStateInfo(layer);
                    if (st.IsTag(LakiAnimatorParams.TagBetReaction))
                    {
                        enteredBet = true;
                        if (st.normalizedTime >= 0.92f) return;
                    }
                    else if (enteredBet)
                        return;
                }

                elapsed += Time.deltaTime;
                await Task.Yield();
            }
        }
    }
}
