using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Book
{
    [DisallowMultipleComponent]
    public class BookCloneAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private RuntimeAnimatorController _bookController;
        [SerializeField] private bool _useErzahlerStyle;

        private bool _initialized;

        public void Configure(RuntimeAnimatorController bookController, bool useErzahlerStyle, Animator animator = null)
        {
            _bookController = bookController;
            _useErzahlerStyle = useErzahlerStyle;
            if (animator != null)
                _animator = animator;
            else if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            _initialized = false;
            TryInitialize();
        }

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
        }

        private void Start() => TryInitialize();

        public void SetMoving(bool isMoving)
        {
            if (_animator == null) return;
            if (_useErzahlerStyle)
            {
                _animator.SetBool(ErzahlerAnimatorParams.Moving, isMoving);
                _animator.SetBool(ErzahlerAnimatorParams.Running, false);
            }
            else
            {
                _animator.SetBool(BookAnimatorParams.Moving, isMoving);
            }
        }

        public void SetWalkVariant(int variant)
        {
            if (_animator == null) return;
            if (_useErzahlerStyle)
                _animator.SetInteger(ErzahlerAnimatorParams.WalkVariant, variant);
            else
                _animator.SetInteger(BookAnimatorParams.WalkVariant, variant);
        }

        public void SetIdleVariant(int variant)
        {
            if (_animator == null) return;
            if (_useErzahlerStyle)
                _animator.SetInteger(ErzahlerAnimatorParams.IdleVariant, variant);
            else
                _animator.SetInteger(BookAnimatorParams.IdleVariant, variant);
        }

        public void BeginCast()
        {
            if (_animator == null) return;
            if (_useErzahlerStyle)
            {
                _animator.ResetTrigger(ErzahlerAnimatorParams.ConjuringFinish);
                _animator.SetBool(ErzahlerAnimatorParams.ConjuringLoop, false);
                _animator.SetTrigger(ErzahlerAnimatorParams.ConjuringPrep);
            }
            else
            {
                _animator.SetTrigger(BookAnimatorParams.Ability);
            }
        }

        public void FinishCast()
        {
            if (_animator == null) return;
            if (_useErzahlerStyle)
            {
                _animator.SetBool(ErzahlerAnimatorParams.ConjuringLoop, false);
                _animator.SetTrigger(ErzahlerAnimatorParams.ConjuringFinish);
            }
            else
            {
                _animator.SetTrigger(BookAnimatorParams.Ability);
            }
        }

        public void CancelCast()
        {
            if (_animator == null) return;
            if (_useErzahlerStyle)
            {
                _animator.SetBool(ErzahlerAnimatorParams.ConjuringLoop, false);
                _animator.SetTrigger(ErzahlerAnimatorParams.ConjuringFinish);
            }
        }

        public void ResetToIdle()
        {
            if (_animator == null) return;
            if (_useErzahlerStyle)
            {
                _animator.SetBool(ErzahlerAnimatorParams.Moving, false);
                _animator.SetBool(ErzahlerAnimatorParams.Running, false);
                _animator.SetInteger(ErzahlerAnimatorParams.WalkVariant, 1);
                _animator.SetInteger(ErzahlerAnimatorParams.IdleVariant, 1);
                _animator.SetBool(ErzahlerAnimatorParams.ConjuringLoop, false);
            }
            else
            {
                _animator.SetBool(BookAnimatorParams.Moving, false);
                _animator.SetInteger(BookAnimatorParams.IdleVariant, 1);
                _animator.SetInteger(BookAnimatorParams.WalkVariant, 1);
            }
        }

        private void TryInitialize()
        {
            if (_initialized || _animator == null || _bookController == null) return;
            _animator.runtimeAnimatorController = _bookController;
            ResetToIdle();
            _initialized = true;
        }
    }
}
