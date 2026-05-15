using System;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.ResourcesLoaderService;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;
using Zenject;
using Logic.Scripts.Turns;
using Logic.Scripts.GameDomain.VisualFeedback;
using Logic.Scripts.GameDomain.Services.Skills;

namespace Logic.Scripts.GameDomain.MVC.Nara {
    // INaraController now extends IPlayableUnit, IEffectable and IEffectableAction,
    // so we no longer need to list those separately here.
    public class NaraController : INaraController, IFixedUpdatable, INextHitDamageShield, ISkillCasterWorldTeleport {
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly IAudioService _audioService;
        private readonly ICommandFactory _commandFactory;
        private readonly NaraView _naraViewPrefab;
        private readonly NaraData _naraData;
        private readonly NaraConfigurationSO _naraConfiguration;
        private readonly ICheatController _cheatController;
        private readonly INewSkillSystemSkillLoadoutService _newSkillSystemSkillLoadoutService;
        public GameObject NaraViewGO => _naraView.gameObject;
        public Transform NaraSkillSpotTransform => _naraView.transform;
        public NaraMovementController NaraMove => _naraMovementController;

        private IGamePlayUiController _gamePlayUiController;
        private NaraView _naraView;
        private NaraMovementController _naraMovementController;
        private int _debuffStacks;
        private bool _canMove;
        private IActionPointsService _actionPointsService;
        private readonly AbilityData[] _abilities;

        private GameObject _activeUnitCircleInstance;
        private bool _hasNextHitShield;

        public NaraController(IUpdateSubscriptionService updateSubscriptionService,
            IAudioService audioService, ICommandFactory commandFactory,
            IResourcesLoaderService resourcesLoaderService, NaraView naraViewPrefab,
            NaraConfigurationSO naraConfiguration, ICheatController cheatController,
            AbilityData[] abilities, [InjectOptional] INewSkillSystemSkillLoadoutService newSkillSystemSkillLoadoutService = null) {
            _naraData = new NaraData(naraConfiguration);
            _naraConfiguration = naraConfiguration;
            _updateSubscriptionService = updateSubscriptionService;
            _audioService = audioService;
            _naraViewPrefab = naraViewPrefab;
            _commandFactory = commandFactory;
            _cheatController = cheatController;
            _abilities = abilities ?? System.Array.Empty<AbilityData>();
            _newSkillSystemSkillLoadoutService = newSkillSystemSkillLoadoutService;
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
            _naraView = UnityEngine.Object.Instantiate(_naraViewPrefab);
            InstallNewSkillSystemSkillComponents();
            _naraData.ResetData();
            _naraView.SetMoving(false);
            _naraMovementController = movementController;

            EnsureActiveUnitCircleCreated();
            SetActiveUnitCircleVisible(false);
        }

        public void ResetController() {
            UnregisterListeners();
            _naraData.ResetData();
            UnityEngine.Object.Destroy(_naraView.gameObject);

            _activeUnitCircleInstance = null;
            _hasNextHitShield = false;
            _gamePlayUiController?.OnPlayerNextHitShieldChanged(false);
        }

        public void InitEntryPointGamePlay(IGamePlayUiController gamePlayUiController) {
            _gamePlayUiController = gamePlayUiController;
            _gamePlayUiController.SetPlayerValues(_naraData.PreviewHealth, _naraData.ActualHealth, _naraConfiguration.MaxHealth);
            _gamePlayUiController.OnPlayerNextHitShieldChanged(false);
            _naraMovementController.InitEntryPoint(_naraView.GetRigidbody(), _naraView.GetCamera());
        }

        /// <inheritdoc />
        public void ApplyCombatLoadoutPassivesAndActionPoints(IActionPointsService actionPoints) {
            SkillDataSO[] slots = _newSkillSystemSkillLoadoutService != null
                ? _newSkillSystemSkillLoadoutService.BuildRuntimeSlotsArray(SkillLoadoutUnitType.Player)
                : null;

            float moveMult = 1f;
            int apTurnBonus = 0;
            if (slots != null) {
                for (int i = 0; i < slots.Length; i++) {
                    SkillDataSO s = slots[i];
                    if (s == null || s.SkillType != SkillType.Passive) continue;

                    PassiveStatModifierEntry[] mods = s.PassiveModifiers;
                    for (int j = 0; j < mods.Length; j++) {
                        PassiveStatModifierEntry e = mods[j];
                        switch (e.Kind) {
                            case PassiveStatModifierKind.MovementRadiusMultiplier:
                                if (e.Value > 0f && !float.IsNaN(e.Value) && !float.IsInfinity(e.Value))
                                    moveMult *= e.Value;
                                break;
                            case PassiveStatModifierKind.ActionPointsTurnGainBonus:
                                apTurnBonus += Mathf.RoundToInt(e.Value);
                                break;
                        }
                    }
                }
            }

            if (actionPoints != null) {
                int max = _naraConfiguration.MaxActionPoints;
                int gain = Mathf.Max(0, _naraConfiguration.ActionPointsTurnGain + apTurnBonus);
                actionPoints.Configure(max, gain);
            }

            if (_naraMovementController is NaraTurnMovementController ntm)
                ntm.ApplyPassiveMovementAreaMultiplier(moveMult);
        }

        public void InitEntryPointExploration() {
            _naraMovementController.InitEntryPoint(_naraView.GetRigidbody(), _naraView.GetCamera());
            Unfreeeze();
        }

        public void SetPosition(Vector3 movementCenter) {
            _naraView.GetRigidbody().position = movementCenter;
        }

        public void TeleportToWorldPosition(Vector3 worldPosition) {
            if (_naraView == null) return;
            var rb = _naraView.GetRigidbody();
            if (rb != null) {
                rb.position = worldPosition;
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }

        #region IEffectable Methods

        public Transform GetReferenceTransform() {
            return _naraView.transform;
        }

        public void ResetPreview() {
            _naraData.ResetPreview();
            _gamePlayUiController?.OnPreviewPlayerHealthUpdate(_naraData.PreviewHealth, _naraConfiguration.MaxHealth);
        }
        public void PreviewDamage(int damageAmound) {
            _naraData.TakeDamage(damageAmound);
            _gamePlayUiController?.OnPreviewPlayerHealthUpdate(_naraData.ActualHealth, _naraConfiguration.MaxHealth);
        }

        public void PreviewHeal(int healAmount) {
            _naraData.ApplyPreviewHeal(healAmount);
            _gamePlayUiController?.OnPreviewPlayerHealthUpdate(_naraData.PreviewHealth, _naraConfiguration.MaxHealth);
        }

        public void GrantNextHitShield() {
            _hasNextHitShield = true;
            _gamePlayUiController?.OnPlayerNextHitShieldChanged(true);
        }

        public void TakeDamage(int damageAmound) {
            if (_cheatController.Imortal == false && damageAmound > 0 && _hasNextHitShield) {
                _hasNextHitShield = false;
                _gamePlayUiController?.OnPlayerNextHitShieldChanged(false);
                return;
            }
            if (_cheatController.Imortal == false) _naraData.TakeDamage(damageAmound);
            if (_naraView != null) {
                var flash = _naraView.GetComponent<DamageFlashPresenter>();
                if (flash == null) flash = _naraView.gameObject.AddComponent<DamageFlashPresenter>();
                flash.TriggerFlash();
            }
            _audioService?.PlayAudio(AudioClipType.AbilityPrep2SFX, AudioChannelType.Fx, AudioPlayType.OneShot);
            _gamePlayUiController?.OnPlayerHealthUpdate(_naraData.ActualHealth, _naraConfiguration.MaxHealth);
            _gamePlayUiController?.OnPreviewPlayerHealthUpdate(_naraData.ActualHealth, _naraConfiguration.MaxHealth);
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
            _naraData.ResetPreview();
            _gamePlayUiController?.OnPlayerHealthUpdate(_naraData.ActualHealth, _naraConfiguration.MaxHealth);
            _gamePlayUiController?.OnPreviewPlayerHealthUpdate(_naraData.ActualHealth, _naraConfiguration.MaxHealth);
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

        public void SetSkillTargetingHighlight(bool active) {
            SkillTargetingHighlightBridge.SetHighlighted(this, active);
        }

        public void Stun(int value) {

        }

        public void SubtractActionPoints(int value) {
            EnsureApService()?.Subtract(value);
        }

        public void SubtractAllActionPoints(int value) {

        }

        public void ReduceActionPointsGainPerTurn(int valueToSubtract, int duration) {

        }

        public void IncreaseActionPointsGainPerTurn(int valueToIncrease, int duration) {

        }

        public void AddActionPoints(int valueToIncrease) {
            EnsureApService()?.Add(valueToIncrease);
        }

        public void ReduceMovementPerTurn(int valueToSubtract, int duration) {

        }

        public void LimitActionPointUse(int value, int duration) {

        }
        #endregion

        private IActionPointsService EnsureApService() {
            if (_actionPointsService != null) return _actionPointsService;
            try {
                var sceneCtxs = UnityEngine.Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None);
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
                if (ProjectContext.Instance != null)
                    _actionPointsService = ProjectContext.Instance.Container.Resolve<IActionPointsService>();
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

        #region IPlayableUnit additional members

        // IPlayableUnit aliases — NaraViewGO / NaraSkillSpotTransform are the canonical
        // Nara properties; UnitViewGO / UnitSkillSpotTransform are the generic aliases.
        public GameObject UnitViewGO => _naraView != null ? _naraView.gameObject : null;
        public Transform UnitSkillSpotTransform => _naraView != null ? _naraView.transform : null;

        public void SetMovementActive(bool isActive) {
            if (_naraMovementController is NaraTurnMovementController ntm)
                ntm.IsActivelyControlled = isActive;
        }

        public void OnBecomeActive() {
            if (_naraMovementController is NaraTurnMovementController ntm)
                ntm.LineHandlerController.SetVisible(true);

            EnsureActiveUnitCircleCreated();
            SetActiveUnitCircleVisible(true);
        }

        public void OnBecomeInactive() {
            if (_naraMovementController is NaraTurnMovementController ntm)
                ntm.LineHandlerController.SetVisible(false);

            SetActiveUnitCircleVisible(false);
        }

        private void EnsureActiveUnitCircleCreated()
        {
            if (_activeUnitCircleInstance != null) return;
            if (_naraView == null) return;
            if (_naraView.ActiveUnitCirclePrefab == null) return;

            _activeUnitCircleInstance = UnityEngine.Object.Instantiate(_naraView.ActiveUnitCirclePrefab, _naraView.transform);
            _activeUnitCircleInstance.name = "ActiveUnitCircle";
            _activeUnitCircleInstance.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            _activeUnitCircleInstance.transform.localRotation = Quaternion.identity;
        }

        private void SetActiveUnitCircleVisible(bool visible)
        {
            if (_activeUnitCircleInstance == null) return;
            _activeUnitCircleInstance.SetActive(visible);
        }

        private void InstallNewSkillSystemSkillComponents()
        {
            if (_naraView == null) return;

            var toggle = _naraView.GetComponent<LegacySkillSystemToggle>();
            if (toggle == null) _naraView.gameObject.AddComponent<LegacySkillSystemToggle>();

            var loadout = _naraView.GetComponent<NewSkillSystemSkillLoadout>();
            if (loadout == null) loadout = _naraView.gameObject.AddComponent<NewSkillSystemSkillLoadout>();
            if (_newSkillSystemSkillLoadoutService != null)
                loadout.SetSkills(_newSkillSystemSkillLoadoutService.BuildRuntimeSlotsArray(SkillLoadoutUnitType.Player));
        }

        public IActionPointsService GetActionPoints() => EnsureApService();
        public AbilityData[] GetAbilities() => _abilities;

        public void SyncArenaMovementAfterMovementSkillDisplacement() {
            if (_naraMovementController is NaraTurnMovementController ntm) {
                ntm.RecenterMovementRingPreservingRadius();
            }
            Unfreeeze();
        }

        public void OnAbilityExecuted() {
            if (_naraMovementController is NaraTurnMovementController ntm) {
                ntm.RecalculateRadiusAfterAbility();
                ntm.SetMovementRadiusCenter();
                ntm.Refresh();
            }
            Unfreeeze();
        }

        public void BeginSkillGuidedDisplacementToWorldPosition(Vector3 worldTarget, float durationSeconds, Action onComplete) {
            if (_naraView == null) {
                onComplete?.Invoke();
                return;
            }
            var displacer = _naraView.GetComponent<ArenaSkillPathDisplacer>();
            if (displacer == null)
                displacer = _naraView.gameObject.AddComponent<ArenaSkillPathDisplacer>();
            displacer.Begin(_naraView.GetRigidbody(), worldTarget, durationSeconds, onComplete);
        }

        #endregion
    }
}