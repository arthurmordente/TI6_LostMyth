using System.Threading.Tasks;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki
{
    public class LakiBossAnimatorView : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        public void SetAnimator(Animator animator) => _animator = animator;

        public void PlayPerformancePrep(int performanceId)
        {
            if (_animator == null) return;
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
            _animator.SetTrigger(LakiAnimatorParams.PerformanceFinish);
        }

        public void PlayAbility()
        {
            if (_animator == null) return;
            _animator.SetTrigger(LakiAnimatorParams.Ability);
        }

        public void PlaySpotlight()
        {
            if (_animator == null) return;
            _animator.SetTrigger(LakiAnimatorParams.Spotlight);
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
    }
}
