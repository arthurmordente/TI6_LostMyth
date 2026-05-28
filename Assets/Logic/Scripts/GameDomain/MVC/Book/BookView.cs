using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Book
{
    public class BookView : MonoBehaviour
    {
        public Transform CastPoint;
        public LineRenderer CastLineRenderer;
        public GameObject TargetPrefab;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Animator _animator;
        [SerializeField] private BookCloneAnimatorDriver _bookAnimatorDriver;

        [Header("Active Unit Circle")]
        [SerializeField] private GameObject _activeUnitCirclePrefab;

        public GameObject ActiveUnitCirclePrefab => _activeUnitCirclePrefab;

        public Rigidbody GetRigidbody() => _rigidbody;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            if (_bookAnimatorDriver == null)
                _bookAnimatorDriver = GetComponent<BookCloneAnimatorDriver>();
        }

        public void ConfigureBookAnimation(RuntimeAnimatorController bookController, bool useErzahlerStyle = false)
        {
            if (_bookAnimatorDriver == null)
                _bookAnimatorDriver = gameObject.AddComponent<BookCloneAnimatorDriver>();
            _bookAnimatorDriver.Configure(bookController, useErzahlerStyle, _animator);
        }

        public void SetMoving(bool isMoving)
        {
            if (_bookAnimatorDriver != null)
            {
                _bookAnimatorDriver.SetMoving(isMoving);
                return;
            }

            if (_animator != null)
                _animator.SetBool(BookAnimatorParams.Moving, isMoving);
        }

        public void PlayDeath()
        {
            // No death clip on Book rig yet.
        }

        public void SetAttackType(int type)
        {
            if (type > 0 && _bookAnimatorDriver != null)
                _bookAnimatorDriver.BeginCast();
            else if (type > 0 && _animator != null)
                _animator.SetTrigger(BookAnimatorParams.Ability);
        }

        public void ResetAttackType()
        {
            _bookAnimatorDriver?.ResetToIdle();
        }

        public void TriggerExecute()
        {
            if (_bookAnimatorDriver != null)
                _bookAnimatorDriver.FinishCast();
            else if (_animator != null)
                _animator.SetTrigger(BookAnimatorParams.Ability);
        }

        public void ResetExecuteTrigger() { }

        public void TriggerCancel()
        {
            _bookAnimatorDriver?.CancelCast();
            ResetAttackType();
        }
    }
}
