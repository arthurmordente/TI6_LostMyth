using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara
{
    public class NaraView : MonoBehaviour
    {
        public Transform CastPoint;
        public LineRenderer CastLineRenderer;
        public GameObject TargetPrefab;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Collider _collider;
        [SerializeField] private Animator _animator;
        [SerializeField] private ErzahlerPlayerAnimatorDriver _erzahlerAnimatorDriver;
        [SerializeField] private ErzahlerPlayerIdleController _erzahlerIdleController;

        [Header("Active Unit Circle")]
        [SerializeField] private GameObject _activeUnitCirclePrefab;

        public GameObject ActiveUnitCirclePrefab => _activeUnitCirclePrefab;

        public Rigidbody GetRigidbody() => _rigidbody;

        public Camera GetCamera() => Camera.main;

        public ErzahlerPlayerAnimatorDriver ErzahlerAnimatorDriver => _erzahlerAnimatorDriver;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            if (_erzahlerAnimatorDriver == null)
                _erzahlerAnimatorDriver = GetComponent<ErzahlerPlayerAnimatorDriver>();
            EnsureErzahlerIdleController();
        }

        public void ConfigureErzahlerAnimation(ErzahlerAnimatorControllersSO controllers)
        {
            if (_erzahlerAnimatorDriver == null)
                _erzahlerAnimatorDriver = gameObject.AddComponent<ErzahlerPlayerAnimatorDriver>();
            _erzahlerAnimatorDriver.Configure(controllers, _animator);
            EnsureErzahlerIdleController();
        }

        private void EnsureErzahlerIdleController()
        {
            if (_erzahlerAnimatorDriver == null) return;
            if (_erzahlerIdleController == null)
                _erzahlerIdleController = GetComponent<ErzahlerPlayerIdleController>();
            if (_erzahlerIdleController == null)
                _erzahlerIdleController = gameObject.AddComponent<ErzahlerPlayerIdleController>();
            _erzahlerIdleController.SetDriver(_erzahlerAnimatorDriver);
        }

        public void SetBookCloneDeployed(bool cloneDeployed)
        {
            _erzahlerAnimatorDriver?.SetBookCloneActive(cloneDeployed);
        }

        public void SetMoving(bool isMoving, bool running = false)
        {
            if (_erzahlerAnimatorDriver != null && _erzahlerAnimatorDriver.UsesErzahlerControllers)
            {
                _erzahlerAnimatorDriver.SetMoving(isMoving, running);
                return;
            }

            if (_animator != null)
                _animator.SetBool("Moving", isMoving);
        }

        public void PlayDeath()
        {
            if (_animator != null)
                _animator.SetTrigger("Dead");
        }

        public void SetAttackType(int type)
        {
            if (_erzahlerAnimatorDriver != null && _erzahlerAnimatorDriver.UsesErzahlerControllers)
            {
                if (type > 0)
                    _erzahlerAnimatorDriver.PlayConjuringSlowPrep();
                return;
            }

            if (_animator != null)
                _animator.SetInteger("AKY_AttackType", type);
        }

        public void ResetAttackType()
        {
            if (_erzahlerAnimatorDriver != null && _erzahlerAnimatorDriver.UsesErzahlerControllers)
            {
                _erzahlerAnimatorDriver.SetConjuringLoop(false);
                return;
            }

            if (_animator != null)
                _animator.SetInteger("AKY_AttackType", 0);
        }

        public void TriggerExecute()
        {
            if (_erzahlerAnimatorDriver != null && _erzahlerAnimatorDriver.UsesErzahlerControllers)
            {
                _erzahlerAnimatorDriver.SetConjuringLoop(true);
                _erzahlerAnimatorDriver.PlayConjuringSlowFinish();
                return;
            }

            if (_animator != null)
                _animator.SetTrigger("Execute");
        }

        public void ResetExecuteTrigger()
        {
            if (_erzahlerAnimatorDriver != null && _erzahlerAnimatorDriver.UsesErzahlerControllers)
                return;

            if (_animator != null)
                _animator.ResetTrigger("Execute");
        }

        public void TriggerCancel()
        {
            if (_erzahlerAnimatorDriver != null && _erzahlerAnimatorDriver.UsesErzahlerControllers)
            {
                _erzahlerAnimatorDriver.CancelConjuring();
                ResetAttackType();
                return;
            }

            if (_animator != null)
                _animator.SetTrigger("Cancel");
        }

        public void ReleaseConjuring()
        {
            _erzahlerAnimatorDriver?.PlayConjuringSlowFinish();
        }

        public LineRenderer GetPointLineRenderer() => CastLineRenderer;
    }
}
