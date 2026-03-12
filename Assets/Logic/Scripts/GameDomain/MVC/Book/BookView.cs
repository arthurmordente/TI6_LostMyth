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

        public Rigidbody GetRigidbody() => _rigidbody;

        public void SetMoving(bool isMoving)
        {
            if (_animator != null) _animator.SetBool("Moving", isMoving);
        }

        public void PlayDeath()
        {
            if (_animator != null) _animator.SetTrigger("Dead");
        }

        public void SetAttackType(int type)
        {
            if (_animator != null) _animator.SetInteger("AKY_AttackType", type);
        }

        public void ResetAttackType()
        {
            if (_animator != null) _animator.SetInteger("AKY_AttackType", 0);
        }

        public void TriggerExecute()
        {
            if (_animator != null) _animator.SetTrigger("Execute");
        }

        public void ResetExecuteTrigger()
        {
            if (_animator != null) _animator.ResetTrigger("Execute");
        }

        public void TriggerCancel()
        {
            if (_animator != null) _animator.SetTrigger("Cancel");
        }
    }
}
