using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Book.Divide
{
    public class DivideAbilityHandler : IDivideAbilityHandler
    {
        private readonly IBookController _bookController;
        private readonly INaraController _naraController;
        private readonly IActiveUnitService _activeUnitService;
        private readonly AbilityData _divideTargetingData;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly ICommandFactory _commandFactory;

        private const int COOLDOWN_TURNS = 1;

        private int _cooldownRemaining;
        private bool _isAiming;
        private bool _targetingSetUp;

        public bool IsBookDeployed => _bookController.IsDeployed;
        public bool IsAiming => _isAiming;
        public int CooldownTurnsRemaining => _cooldownRemaining;

        public DivideAbilityHandler(
            IBookController bookController,
            INaraController naraController,
            IActiveUnitService activeUnitService,
            IUpdateSubscriptionService updateSubscriptionService,
            ICommandFactory commandFactory,
            AbilityData divideTargetingData)
        {
            _bookController = bookController;
            _naraController = naraController;
            _activeUnitService = activeUnitService;
            _updateSubscriptionService = updateSubscriptionService;
            _commandFactory = commandFactory;
            _divideTargetingData = divideTargetingData;
        }

        public void Activate()
        {
            if (_cooldownRemaining > 0) return;

            if (IsBookDeployed) {
                RecallBook();
            } else if (_isAiming) {
                // Already showing the placement preview — do nothing.
                // The existing indicator already follows the mouse; no need to spawn a second one.
                return;
            } else {
                StartAiming();
            }
        }

        public void ConfirmPlacement()
        {
            if (!_isAiming) return;
            if (_divideTargetingData == null) return;

            // LockAim() returns the world-space point selected by the targeting strategy
            // and cleans up the targeting visuals automatically.
            IEffectable[] targets;
            Vector3 spawnPos = _divideTargetingData.TargetingStrategy.LockAim(out targets);

            _isAiming = false;
            DeployBook(spawnPos);
        }

        public void CancelAim()
        {
            if (!_isAiming) return;
            _divideTargetingData?.Cancel();
            _isAiming = false;
        }

        public void OnPlayerTurnStart()
        {
            // Tick cooldown
            if (_cooldownRemaining > 0)
                _cooldownRemaining--;

            // Always give control back to Nara at the start of each player turn.
            // The player can TAB to the book again during the turn.
            _activeUnitService?.SetNaraAsActiveUnit();

            if (IsBookDeployed)
            {
                _bookController.GainTurnActionPoints();
                _bookController.ResetMovementArea();
            }
        }

        public void OnPlayerTurnEnd()
        {
            // Ensure control returns to Nara and the book's line is hidden
            // before the echo / environment phases start.
            _activeUnitService?.SetNaraAsActiveUnit();

            // Cancel any in-progress aim if the turn ended mid-aim
            if (_isAiming) CancelAim();
        }

        // ── private helpers ─────────────────────────────────────────────────────

        private void StartAiming()
        {
            if (_divideTargetingData == null)
            {
                Debug.LogWarning("[DivideAbility] No targeting data assigned. Assign a PointTargetingAbilityData in the GamePlayInstaller.");
                return;
            }

            // SetUp only once — initialises the TargetingStrategy's update/command subscriptions
            if (!_targetingSetUp)
            {
                _divideTargetingData.SetUp(_updateSubscriptionService, _commandFactory);
                _targetingSetUp = true;
            }

            _isAiming = true;
            // Initialize the targeting strategy — this shows the targeting cursor/indicator
            _divideTargetingData.Aim(_naraController);
        }

        private void DeployBook(Vector3 position)
        {
            _bookController.CreateBook(position);
            _activeUnitService.RegisterBook(_bookController);
            _cooldownRemaining = COOLDOWN_TURNS;
        }

        private void RecallBook()
        {
            _activeUnitService.SetNaraAsActiveUnit();
            _activeUnitService.UnregisterBook();
            _bookController.DestroyBook();
            _cooldownRemaining = 0;
        }
    }
}
