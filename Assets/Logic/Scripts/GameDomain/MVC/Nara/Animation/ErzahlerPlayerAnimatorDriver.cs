using System.Threading.Tasks;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Animation
{
    /// <summary>
    /// Drives the player Erzahler Animator: swaps between combined (with book) and solo controllers
    /// when the Book clone is deployed via Divide.
    /// </summary>
    [DisallowMultipleComponent]
    public class ErzahlerPlayerAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private ErzahlerAnimatorControllersSO _controllers;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _bookVisualRoot;
        [SerializeField] private bool _hideBookMeshWhenCloneDeployed = true;

        private bool _cloneDeployed;
        private bool _initialized;

        public bool UsesErzahlerControllers =>
            _controllers != null &&
            _controllers.ErzahlerWithBook != null &&
            _controllers.ErzahlerSolo != null;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            TryFindBookVisualRoot();
        }

        private void Start()
        {
            InitializeDefaultController();
        }

        public void Configure(ErzahlerAnimatorControllersSO controllers, Animator animator = null)
        {
            _controllers = controllers;
            if (animator != null)
                _animator = animator;
            else if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            _initialized = false;
            InitializeDefaultController();
        }

        public void SetBookCloneActive(bool cloneDeployed)
        {
            _cloneDeployed = cloneDeployed;
            ApplyControllerSwap();
            UpdateBookVisualVisibility();
        }

        public void SetMoving(bool isMoving, bool running = false)
        {
            if (_animator == null || !UsesErzahlerControllers) return;

            _animator.SetBool(ErzahlerAnimatorParams.Moving, isMoving);
            _animator.SetBool(ErzahlerAnimatorParams.Running, running);
        }

        public void SetWalkVariant(int variant)
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            _animator.SetInteger(ErzahlerAnimatorParams.WalkVariant, variant);
        }

        public void SetIdleVariant(int variant)
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            _animator.SetInteger(ErzahlerAnimatorParams.IdleVariant, variant);
        }

        public int GetIdleVariant()
        {
            if (_animator == null || !UsesErzahlerControllers) return 1;
            return _animator.GetInteger(ErzahlerAnimatorParams.IdleVariant);
        }

        public void PlayConjuringFast()
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            _animator.SetTrigger(ErzahlerAnimatorParams.ConjuringFast);
        }

        public void PlayConjuringSlowPrep()
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            _animator.ResetTrigger(ErzahlerAnimatorParams.ConjuringFinish);
            _animator.ResetTrigger(ErzahlerAnimatorParams.ConjuringCancel);
            _animator.SetBool(ErzahlerAnimatorParams.ConjuringLoop, true);
            _animator.SetTrigger(ErzahlerAnimatorParams.ConjuringPrep);
        }

        public void SetConjuringLoop(bool looping)
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            _animator.SetBool(ErzahlerAnimatorParams.ConjuringLoop, looping);
        }

        public void PlayConjuringSlowFinish()
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            _animator.SetBool(ErzahlerAnimatorParams.ConjuringLoop, false);
            _animator.ResetTrigger(ErzahlerAnimatorParams.ConjuringCancel);
            _animator.SetTrigger(ErzahlerAnimatorParams.ConjuringFinish);
        }

        public void CancelConjuring()
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            _animator.SetBool(ErzahlerAnimatorParams.ConjuringLoop, false);
            _animator.ResetTrigger(ErzahlerAnimatorParams.ConjuringFinish);
            _animator.SetTrigger(ErzahlerAnimatorParams.ConjuringCancel);
        }

        public void PlayConjuringFail()
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            if (!HasAnimatorParameter(ErzahlerAnimatorParams.ConjuringFail)) return;
            _animator.SetTrigger(ErzahlerAnimatorParams.ConjuringFail);
        }

        public void PlayDeath()
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            if (!HasAnimatorParameter(ErzahlerAnimatorParams.Dead)) return;
            _animator.SetTrigger(ErzahlerAnimatorParams.Dead);
        }

        public void PlayHit()
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            if (!HasAnimatorParameter(ErzahlerAnimatorParams.Hit)) return;
            _animator.SetTrigger(ErzahlerAnimatorParams.Hit);
        }

        public void PlayBetReaction(bool playerWon)
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            string param = playerWon ? ErzahlerAnimatorParams.BetWon : ErzahlerAnimatorParams.BetLost;
            if (!HasAnimatorParameter(param)) return;
            _animator.SetTrigger(param);
        }

        public void PlayDivideDeploy()
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            if (!HasAnimatorParameter(ErzahlerAnimatorParams.DivideDeploy)) return;
            _animator.SetTrigger(ErzahlerAnimatorParams.DivideDeploy);
        }

        public void PlayDivideRecall()
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            if (!HasAnimatorParameter(ErzahlerAnimatorParams.DivideRecall)) return;
            _animator.SetTrigger(ErzahlerAnimatorParams.DivideRecall);
        }

        public bool HasAnimatorParameter(string name)
        {
            if (_animator == null || string.IsNullOrEmpty(name)) return false;
            var parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name) return true;
            }

            return false;
        }

        public async Task WaitUntilBetReactionCompleteAsync(float timeoutSeconds = 5f, int layer = 0)
        {
            if (_animator == null || !UsesErzahlerControllers) return;
            if (!HasAnimatorParameter(ErzahlerAnimatorParams.BetWon)
                && !HasAnimatorParameter(ErzahlerAnimatorParams.BetLost))
                return;

            float elapsed = 0f;
            bool enteredBet = false;
            while (elapsed < Mathf.Max(0.01f, timeoutSeconds))
            {
                if (!_animator.IsInTransition(layer))
                {
                    var st = _animator.GetCurrentAnimatorStateInfo(layer);
                    if (st.IsTag(ErzahlerAnimatorParams.TagBetReaction))
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

        private void InitializeDefaultController()
        {
            _controllers ??= ErzahlerAnimatorControllersSO.LoadDefault();
            if (_initialized || _animator == null || _controllers == null) return;
            if (_controllers.ErzahlerWithBook == null) return;

            _cloneDeployed = false;
            _animator.runtimeAnimatorController = _controllers.ErzahlerWithBook;
            _initialized = true;
            UpdateBookVisualVisibility();
        }

        private void ApplyControllerSwap()
        {
            if (_animator == null || _controllers == null) return;

            var next = _cloneDeployed ? _controllers.ErzahlerSolo : _controllers.ErzahlerWithBook;
            if (next == null) return;

            _animator.runtimeAnimatorController = next;
        }

        private void UpdateBookVisualVisibility()
        {
            if (!_hideBookMeshWhenCloneDeployed || _bookVisualRoot == null) return;
            _bookVisualRoot.gameObject.SetActive(!_cloneDeployed);
        }

        private void TryFindBookVisualRoot()
        {
            if (_bookVisualRoot != null) return;
            var rootBook = transform.Find("ErzahlerArmature/ROOTBook");
            if (rootBook != null)
                _bookVisualRoot = rootBook;
        }
    }
}
