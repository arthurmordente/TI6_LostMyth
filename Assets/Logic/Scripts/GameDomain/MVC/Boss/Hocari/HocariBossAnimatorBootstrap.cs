using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Hocari
{
    /// <summary>Adds Hocari animation bridge/view to Hokari boss rigs at runtime or in editor.</summary>
    public sealed class HocariBossAnimatorBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var animator = GetComponentInChildren<Animator>(true);
            if (animator == null) return;

            var view = GetComponent<HocariBossAnimatorView>();
            if (view == null)
                view = gameObject.AddComponent<HocariBossAnimatorView>();
            view.SetAnimator(animator);

            var bridge = GetComponent<HocariBossAnimationBridge>();
            if (bridge == null)
                bridge = gameObject.AddComponent<HocariBossAnimationBridge>();
        }
    }
}
