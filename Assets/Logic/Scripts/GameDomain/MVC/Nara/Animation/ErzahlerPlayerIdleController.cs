using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Animation
{
    /// <summary>
    /// Alternates player idle variants while not moving and not casting.
    /// Works on Erzahler solo (Idle_1 / Idle_2) when using ErzahlerAnimatorControllersSO.
    /// </summary>
    [DisallowMultipleComponent]
    public class ErzahlerPlayerIdleController : MonoBehaviour
    {
        [SerializeField] private ErzahlerPlayerAnimatorDriver _driver;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private float _intervalSeconds = 8f;
        [SerializeField] private bool _alternateIdle = true;
        [SerializeField] private bool _pauseWhileCasting = true;

        private float _nextSwitchTime;

        private void Awake()
        {
            if (_driver == null)
                _driver = GetComponent<ErzahlerPlayerAnimatorDriver>();
        }

        private void OnEnable()
        {
            _nextSwitchTime = Time.time + _intervalSeconds;
        }

        private void OnDisable()
        {
            _nextSwitchTime = float.PositiveInfinity;
        }

        private void Update()
        {
            if (!_enabled || _driver == null || !_driver.UsesErzahlerControllers)
                return;

            if (IsCasting() || IsMoving())
                return;

            if (Time.time < _nextSwitchTime)
                return;

            _nextSwitchTime = Time.time + _intervalSeconds;
            ToggleIdle();
        }

        public void SetDriver(ErzahlerPlayerAnimatorDriver driver)
        {
            _driver = driver;
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled)
                _nextSwitchTime = float.PositiveInfinity;
        }

        private Animator ResolveAnimator()
        {
            if (_driver == null) return null;
            return _driver.GetComponent<Animator>();
        }

        private bool IsMoving()
        {
            var anim = ResolveAnimator();
            if (anim == null) return false;
            return anim.GetBool(ErzahlerAnimatorParams.Moving);
        }

        private bool IsCasting()
        {
            if (!_pauseWhileCasting || _driver == null || !_driver.UsesErzahlerControllers)
                return false;

            var anim = ResolveAnimator();
            if (anim == null) return false;

            if (anim.GetBool(ErzahlerAnimatorParams.ConjuringLoop))
                return true;

            var state = anim.GetCurrentAnimatorStateInfo(0);
            if (state.IsTag(ErzahlerAnimatorParams.TagConjuringLoop))
                return true;

            return state.IsName("Prep") || state.IsName("Loop") || state.IsName("Finish");
        }

        private void ToggleIdle()
        {
            int current = _driver.GetIdleVariant();
            int next = current <= 1 ? 2 : 1;
            _driver.SetIdleVariant(next);
        }
    }
}
