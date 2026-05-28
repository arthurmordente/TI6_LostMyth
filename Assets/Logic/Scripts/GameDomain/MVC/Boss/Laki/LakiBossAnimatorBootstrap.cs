using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki
{
    /// <summary>
    /// Ensures the Laki boss Animator uses LKI_Animator and runtime animation drivers.
    /// </summary>
    public class LakiBossAnimatorBootstrap : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private ErzahlerAnimatorControllersSO _controllers;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            var lakiController = ResolveLakiController();
            if (_animator != null && lakiController != null)
                _animator.runtimeAnimatorController = lakiController;

            var view = GetComponent<LakiBossAnimatorView>();
            if (view == null)
                view = gameObject.AddComponent<LakiBossAnimatorView>();
            if (_animator != null)
                view.SetAnimator(_animator);

            if (GetComponent<LakiBossAnimationBridge>() == null)
                gameObject.AddComponent<LakiBossAnimationBridge>();
        }

        private RuntimeAnimatorController ResolveLakiController()
        {
            _controllers ??= ErzahlerAnimatorControllersSO.LoadDefault();
            if (_controllers != null && _controllers.LakiBoss != null)
                return _controllers.LakiBoss;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Art/Animations/MadamLaki/LKI_Animator.controller");
#else
            return null;
#endif
        }
    }
}
