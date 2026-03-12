using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.Services.UpdateService;
using Logic.Scripts.Turns;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Book
{
    public class BookController : IBookController, IFixedUpdatable
    {
        private readonly BookView _bookViewPrefab;
        private readonly NaraConfigurationSO _config;
        private readonly global::GameInputActions _gameInputActions;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly ICheatController _cheatController;
        // The Book's own ability set — configured separately in the inspector.
        // Initially points to the same abilities as Nara; swap to a dedicated array to diverge.
        private readonly AbilityData[] _abilities;

        private BookView _bookView;
        private NaraTurnMovementController _movementController;
        private BookData _bookData;
        private BookActionPoints _bookActionPoints;
        private bool _canMove;
        private bool _isDeployed;

        public bool IsDeployed => _isDeployed;
        public GameObject UnitViewGO => _bookView != null ? _bookView.gameObject : null;
        public Transform UnitSkillSpotTransform => _bookView != null ? _bookView.transform : null;

        public BookController(
            BookView bookViewPrefab,
            NaraConfigurationSO config,
            AbilityData[] abilities,
            global::GameInputActions gameInputActions,
            IUpdateSubscriptionService updateSubscriptionService,
            ICheatController cheatController)
        {
            _bookViewPrefab = bookViewPrefab;
            _config = config;
            _abilities = abilities ?? System.Array.Empty<AbilityData>();
            _gameInputActions = gameInputActions;
            _updateSubscriptionService = updateSubscriptionService;
            _cheatController = cheatController;
        }

        public void CreateBook(Vector3 position)
        {
            // Instantiate directly at the target position so that the Rigidbody's internal
            // physics position is set correctly from the start.  Setting transform.position
            // after a plain Instantiate() only moves the Transform; the Rigidbody's position
            // stays at the spawn origin and the physics loop snaps the object back next FixedUpdate.
            _bookView = Object.Instantiate(_bookViewPrefab, position, Quaternion.identity);

            // Extra guarantee: zero out any velocity the prefab might carry and lock the
            // Rigidbody position so physics doesn't drift before DeactivateNaraGravity runs.
            var rb = _bookView.GetRigidbody();
            if (rb != null)
            {
                rb.position = position;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            _bookData = new BookData(_config);
            _bookActionPoints = new BookActionPoints(_config.MaxActionPoints, _config.ActionPointsTurnGain);

            _movementController = new NaraTurnMovementController(
                _gameInputActions,
                _updateSubscriptionService,
                _config,
                _cheatController);

            _movementController.InitEntryPoint(_bookView.GetRigidbody(), Camera.main);
            _movementController.IsActivelyControlled = false;
            _movementController.DeactivateNaraGravity();

            _isDeployed = true;
            _canMove = true;

            _updateSubscriptionService.RegisterFixedUpdatable(this);
        }

        public void DestroyBook()
        {
            if (!_isDeployed) return;

            _isDeployed = false;
            _canMove = false;

            _updateSubscriptionService.UnregisterFixedUpdatable(this);

            if (_movementController != null)
            {
                _movementController.IsActivelyControlled = false;
            }

            if (_bookView != null)
            {
                Object.Destroy(_bookView.gameObject);
                _bookView = null;
            }

            _movementController = null;
            _bookData = null;
            _bookActionPoints = null;
        }

        public void ResetMovementArea()
        {
            _movementController?.ResetMovementArea();
        }

        public void GainTurnActionPoints()
        {
            _bookActionPoints?.GainTurnPoints();
        }

        #region IPlayableUnit

        public void Freeeze() => _canMove = false;
        public void Unfreeeze() => _canMove = true;

        public void FreezeInputs()
        {
            _canMove = false;
            try { _movementController?.DisableInputs(); } catch { }
        }

        public void UnfreezeInputs()
        {
            _canMove = true;
            try { _movementController?.EnableInputs(); } catch { }
        }

        public void StopMovingAnim() => _bookView?.SetMoving(false);
        public void PlayAttackType(int type) => _bookView?.SetAttackType(type);

        public void TriggerExecute() => _bookView?.TriggerExecute();
        public void ResetExecuteTrigger() => _bookView?.ResetExecuteTrigger();
        public void TriggerCancel()
        {
            _bookView?.TriggerCancel();
            _bookView?.ResetAttackType();
        }

        public void SetMovementActive(bool isActive)
        {
            if (_movementController != null)
                _movementController.IsActivelyControlled = isActive;
        }

        // Returns the Book's own AP pool so its abilities cost from it, not from Nara's pool.
        public IActionPointsService GetActionPoints() => _bookActionPoints;
        public AbilityData[] GetAbilities() => _abilities;

        public void OnAbilityExecuted()
        {
            if (_movementController == null) return;
            _movementController.RecalculateRadiusAfterAbility();
            _movementController.SetMovementRadiusCenter();
            _movementController.Refresh();
            Unfreeeze();
        }

        public void OnBecomeActive()
        {
            if (_movementController != null)
                _movementController.LineHandlerController.SetVisible(true);
        }

        public void OnBecomeInactive()
        {
            if (_movementController != null)
                _movementController.LineHandlerController.SetVisible(false);
        }

        #endregion

        #region IEffectable

        public Transform GetReferenceTransform() => _bookView != null ? _bookView.transform : null;
        public Transform GetTransformCastPoint() => _bookView != null ? _bookView.CastPoint : null;
        public GameObject GetReferenceTargetPrefab() => _bookView != null ? _bookView.TargetPrefab : null;
        public LineRenderer GetPointLineRenderer() => _bookView != null ? _bookView.CastLineRenderer : null;

        public void ResetPreview() => _bookData?.ResetPreview();

        public void PreviewDamage(int amount)
        {
            _bookData?.TakeDamage(amount);
        }

        public void PreviewHeal(int amount)
        {
            _bookData?.TakeDamage(-amount);
        }

        public void TakeDamage(int amount)
        {
            if (_bookData == null) return;
            _bookData.TakeDamage(amount);
        }

        public void TakeDamagePerTurn(int damageAmount, int duration) { }
        public void Heal(int amount) { if (_bookData != null) _bookData.Heal(amount); }
        public void HealPerTurn(int healAmount, int duration) { }

        #endregion

        #region IEffectableAction

        public void Stun(int value) { }
        public void SubtractActionPoints(int value) => _bookActionPoints?.Subtract(value);
        public void SubtractAllActionPoints(int value) { }
        public void ReduceActionPointsGainPerTurn(int valueToSubtract, int duration) { }
        public void IncreaseActionPointsGainPerTurn(int valueToIncrease, int duration) { }
        public void AddActionPoints(int valueToIncrease) => _bookActionPoints?.Add(valueToIncrease);
        public void ReduceMovementPerTurn(int valueToSubtract, int duration) { }
        public void LimitActionPointUse(int value, int duration) { }

        #endregion

        #region IFixedUpdatable

        public void ManagedFixedUpdate()
        {
            if (!_isDeployed || _movementController == null) return;

            Vector2 dir = _movementController.ReadInputs();
            if (dir == Vector2.zero || !_canMove)
            {
                _movementController.Move(Vector2.zero, 0f, 0f);
                _bookView?.SetMoving(false);
            }
            else
            {
                bool movementAllowed = _movementController.IsMovementAllowed();
                _movementController.Move(dir, _config.MoveSpeed, _config.RotationSpeed);
                _bookView?.SetMoving(movementAllowed && dir.sqrMagnitude > 0.0001f);
            }
        }

        public void RegisterListeners() => _updateSubscriptionService.RegisterFixedUpdatable(this);
        public void UnregisterListeners() => _updateSubscriptionService.UnregisterFixedUpdatable(this);

        #endregion
    }
}
