using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.ResourcesLoaderService;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;
using Zenject;
using Logic.Scripts.Turns;

namespace Logic.Scripts.GameDomain.MVC.Nara {
    public class NaraController : INaraController, IFixedUpdatable, IEffectable, IEffectableAction {
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly IAudioService _audioService;
        private readonly ICommandFactory _commandFactory;
        private readonly NaraView _naraViewPrefab;
        private readonly NaraData _naraData;
        private readonly NaraConfigurationSO _naraConfiguration;
        public GameObject NaraViewGO => _naraView.gameObject;
        public Transform NaraSkillSpotTransform => _naraView.transform;
        public NaraMovementController NaraMove => _naraMovementController;

        private IGamePlayUiController _gamePlayUiController;
        private NaraView _naraView;
        private NaraMovementController _naraMovementController;
        private int _debuffStacks;
        private bool _canMove;
        private IActionPointsService _actionPointsService;

        public NaraController(IUpdateSubscriptionService updateSubscriptionService,
            IAudioService audioService, ICommandFactory commandFactory,
            IResourcesLoaderService resourcesLoaderService, NaraView naraViewPrefab,
            NaraConfigurationSO naraConfiguration) {
            _naraData = new NaraData(naraConfiguration);
            _naraConfiguration = naraConfiguration;
            _updateSubscriptionService = updateSubscriptionService;
            _audioService = audioService;
            _naraViewPrefab = naraViewPrefab;
            _commandFactory = commandFactory;
        }

        public void RegisterListeners() {
            _updateSubscriptionService.RegisterFixedUpdatable(this);
        }

        public void UnregisterListeners() {
            _updateSubscriptionService.UnregisterFixedUpdatable(this);
        }

        public void StopMovingAnim() {
            _naraView?.SetMoving(false);
        }
        public void Freeeze() {
            _canMove = false;
        }

        public void Unfreeeze() {
            _canMove = true;
        }

        public void FreezeInputs() {
            _canMove = false;
            try { _naraMovementController?.DisableInputs(); } catch { }
        }

        public void UnfreezeInputs() {
            _canMove = true;
            try { _naraMovementController?.EnableInputs(); } catch { }
        }

        public void ManagedFixedUpdate() {
            Vector2 dir = _naraMovementController.ReadInputs();
            bool movementAllowed = true;
            if (_naraMovementController is NaraTurnMovementController ntm) {
                movementAllowed = ntm.IsMovementAllowed();
            }
            if (dir == Vector2.zero || _canMove == false) {
                _naraMovementController.Move(Vector2.zero, 0f, 0f);
                _naraView?.SetMoving(false);
            }
            else {
                _naraMovementController.Move(dir, _naraConfiguration.MoveSpeed, _naraConfiguration.RotationSpeed);
                bool willMove = movementAllowed && dir.sqrMagnitude > 0.0001f && _naraConfiguration.MoveSpeed > 0f;
                _naraView?.SetMoving(willMove);
            }
        }

        public void CreateNara(NaraMovementController movementController) {
            _naraView = Object.Instantiate(_naraViewPrefab);
            _naraData.ResetData();
            _naraView.SetMoving(false);
            _naraMovementController = movementController;
        }

        public void ResetController() {
            UnregisterListeners();
            _naraData.ResetData();
            UnityEngine.Object.Destroy(_naraView.gameObject);
        }

        public void InitEntryPointGamePlay(IGamePlayUiController gamePlayUiController) {
            _gamePlayUiController = gamePlayUiController;
            _gamePlayUiController.SetPlayerValues(_naraData.ActualHealth, _naraData.PreviewHealth);
            _naraMovementController.InitEntryPoint(_naraView.GetRigidbody(), _naraView.GetCamera());
        }

        public void InitEntryPointExploration() {
            _naraMovementController.InitEntryPoint(_naraView.GetRigidbody(), _naraView.GetCamera());
            Unfreeeze();
        }

        public void SetPosition(Vector3 movementCenter) {
            _naraView.GetRigidbody().position = movementCenter;
        }

        #region IEffectable Methods

        public Transform GetReferenceTransform() {
            return _naraView.transform;
        }

        public void ResetPreview() {
            _naraData.ResetPreview();
        }
        public void PreviewDamage(int damageAmound) {
            _naraData.TakeDamage(damageAmound);
            _gamePlayUiController.OnPreviewPlayerLifePercentChange(_naraData.ActualHealth);
        }

        public void PreviewHeal(int damageAmound) {
            _naraData.TakeDamage(damageAmound);
            _gamePlayUiController.OnPreviewPlayerLifePercentChange(_naraData.ActualHealth);
        }

        public void TakeDamage(int damageAmound) {
            _naraData.TakeDamage(damageAmound);
            _audioService?.PlayAudio(AudioClipType.AbilityPrep2SFX, AudioChannelType.Fx);
            _gamePlayUiController.OnActualPlayerHealthChange(_naraData.ActualHealth);
            _gamePlayUiController.OnActualPlayerLifePercentChange(_naraData.ActualHealth);
            _gamePlayUiController.OnPreviewPlayerLifePercentChange(_naraData.ActualHealth);
            if (_naraData.IsAlive()) {
                _naraView?.PlayDeath();
                _commandFactory.CreateCommandVoid<GameOverCommand>().SetData(new GameOverCommandData(false)).Execute();
            }
        }

        public void PlayAttackType(int type) {
            _naraView?.SetAttackType(type);
        }

        public void PlayAttackType1() {
            _naraView?.SetAttackType(1);
        }

        public void Heal(int healAmount) {
            _naraData.Heal(healAmount);
            _gamePlayUiController.OnActualPlayerHealthChange(_naraData.ActualHealth);
            _gamePlayUiController.OnActualPlayerLifePercentChange(_naraData.ActualHealth);
        }

        public void TriggerExecute() {
            _audioService.PlayAudio(AudioClipType.AbilityUsed1SFX, AudioChannelType.Fx);
            _naraView?.TriggerExecute();
        }

        public void ResetExecuteTrigger() {
            _naraView?.ResetExecuteTrigger();
        }

        public void TriggerCancel() {
            _naraView?.TriggerCancel();
            _naraView?.ResetAttackType();
        }

        public void TakeDamagePerTurn(int damageAmount, int duration) {

        }

        public void HealPerTurn(int healAmount, int duration) {

        }

        public void Stun(int value) {

        }

        public void SubtractActionPoints(int value) {
            var ap = EnsureApService();
            ap?.Subtract(value);
        }

        public void SubtractAllActionPoints(int value) {

        }

        public void ReduceActionPointsGainPerTurn(int valueToSubtract, int duration) {

        }

        public void IncreaseActionPointsGainPerTurn(int valueToIncrease, int duration) {

        }

        public void AddActionPoints(int valueToIncrease) {
            var ap = EnsureApService();
            ap?.Add(valueToIncrease);
        }

        public void ReduceMovementPerTurn(int valueToSubtract, int duration) {

        }

        public void LimitActionPointUse(int value, int duration) {

        }
        #endregion

        private IActionPointsService EnsureApService() {
            if (_actionPointsService != null) return _actionPointsService;
            try {
                var sceneCtxs = Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneCtxs.Length; i++) {
                    var sc = sceneCtxs[i];
                    if (sc != null) {
                        _actionPointsService = sc.Container.Resolve<IActionPointsService>();
                        if (_actionPointsService != null) return _actionPointsService;
                    }
                }
            }
            catch { }
            try {
                if (ProjectContext.Instance != null) {
                    _actionPointsService = ProjectContext.Instance.Container.Resolve<IActionPointsService>();
                }
            }
            catch { }
            return _actionPointsService;
        }

        // Debuff API
        public int GetNumberDebuffs() {
            return _debuffStacks;
        }

        public void AddDebuffStacks(int amount) {
            if (amount <= 0) return;
            _debuffStacks += amount;
            Debug.Log($"Nara debuff stacks updated: {_debuffStacks} (+{amount})");
        }

        public int GetDebuffStacks() {
            return _debuffStacks;
        }

        public Transform GetTransformCastPoint() {
            return _naraView.CastPoint;
        }

        public GameObject GetReferenceTargetPrefab() {
            return _naraView.TargetPrefab;
        }

        public LineRenderer GetPointLineRenderer() {
            return _naraView.CastLineRenderer;
        }
    }
}