using System;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Environment;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.ResourcesLoaderService;
using Logic.Scripts.Services.UpdateService;
using Logic.Scripts.Turns;
using UnityEngine;
using Zenject;
using Logic.Scripts.GameDomain.VisualFeedback;
using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers;

namespace Logic.Scripts.GameDomain.MVC.Nara {    // INaraController now extends IPlayableUnit, IEffectable and IEffectableAction,
    // so we no longer need to list those separately here.
    public class NaraController : INaraController, IFixedUpdatable, INextHitDamageShield, IOutgoingDamageModifier, IOutgoingDamageLifesteal, ISkillCasterWorldTeleport {
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly IAudioService _audioService;
        private readonly ICommandFactory _commandFactory;
        private readonly NaraView _naraViewPrefab;
        private readonly NaraData _naraData;
        private readonly NaraConfigurationSO _naraConfiguration;
        private readonly ICheatController _cheatController;
        private readonly INewSkillSystemSkillLoadoutService _newSkillSystemSkillLoadoutService;
        private readonly ErzahlerAnimatorControllersSO _erzahlerAnimatorControllers;
        private readonly IDamageStackMovementPassiveService _damageStackMovementPassiveService;
        public GameObject NaraViewGO => _naraView.gameObject;
        public Transform NaraSkillSpotTransform => _naraView.transform;
        public NaraMovementController NaraMove => _naraMovementController;
        public int CurrentHealth => _naraData.ActualHealth;
        public int MaxHealth => _naraConfiguration.MaxHealth;

        private IGamePlayUiController _gamePlayUiController;
        private NaraView _naraView;
        private NaraMovementController _naraMovementController;
        private int _debuffStacks;
        private bool _canMove;
        private IActionPointsService _actionPointsService;
        private readonly AbilityData[] _abilities;

        private GameObject _activeUnitCircleInstance;
        private bool _hasNextHitShield;
        private bool _hasNextOutgoingDamageModifier;
        private float _nextOutgoingDamageMultiplier = 1f;
        private float _outgoingLifestealPercent;
        private bool _footstepsPlaying;
        private const float FootstepLoopSegmentSeconds = 0.5f;

        public NaraController(IUpdateSubscriptionService updateSubscriptionService,
            IAudioService audioService, ICommandFactory commandFactory,
            IResourcesLoaderService resourcesLoaderService, NaraView naraViewPrefab,
            NaraConfigurationSO naraConfiguration, ICheatController cheatController,
            AbilityData[] abilities,
            [InjectOptional] IActionPointsService actionPointsService = null,
            [InjectOptional] INewSkillSystemSkillLoadoutService newSkillSystemSkillLoadoutService = null,
            [InjectOptional] ErzahlerAnimatorControllersSO erzahlerAnimatorControllers = null,
            [InjectOptional] IDamageStackMovementPassiveService damageStackMovementPassiveService = null) {
            _naraData = new NaraData(naraConfiguration);
            _naraConfiguration = naraConfiguration;
            _updateSubscriptionService = updateSubscriptionService;
            _audioService = audioService;
            _naraViewPrefab = naraViewPrefab;
            _commandFactory = commandFactory;
            _cheatController = cheatController;
            _abilities = abilities ?? System.Array.Empty<AbilityData>();
            _actionPointsService = actionPointsService;
            _newSkillSystemSkillLoadoutService = newSkillSystemSkillLoadoutService;
            _erzahlerAnimatorControllers = erzahlerAnimatorControllers;
            _damageStackMovementPassiveService = damageStackMovementPassiveService;
        }

        public void RegisterListeners() {
            _updateSubscriptionService.RegisterFixedUpdatable(this);
        }

        public void UnregisterListeners() {
            _updateSubscriptionService.UnregisterFixedUpdatable(this);
        }

        public void StopMovingAnim() {
            _naraView?.SetMoving(false);
            StopFootstepSfx();
        }
        public void Freeeze() {
            _canMove = false;
            StopFootstepSfx();
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
                StopFootstepSfx();
            }
            else {
                _naraMovementController.Move(dir, _naraConfiguration.MoveSpeed, _naraConfiguration.RotationSpeed);
                bool willMove = movementAllowed && dir.sqrMagnitude > 0.0001f && _naraConfiguration.MoveSpeed > 0f;
                bool running = willMove && _naraConfiguration.MoveSpeed >= _naraConfiguration.JogSpeedThreshold;
                _naraView?.SetMoving(willMove, running);
                if (willMove)
                    StartFootstepSfx();
                else
                    StopFootstepSfx();
            }
        }

        public void CreateNara(NaraMovementController movementController) {
            _naraView = UnityEngine.Object.Instantiate(_naraViewPrefab);
            InstallNewSkillSystemSkillComponents();
            if (_erzahlerAnimatorControllers != null)
                _naraView.ConfigureErzahlerAnimation(_erzahlerAnimatorControllers);
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
            _hasNextOutgoingDamageModifier = false;
            _nextOutgoingDamageMultiplier = 1f;
            _outgoingLifestealPercent = 0f;
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
                _actionPointsService = actionPoints;
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

        public void BeginSelfDamageCastAimPreviewFromSkill(SkillDataSO skill) {
            if (skill == null || _gamePlayUiController == null) return;
            if (!SkillCastSelfDamagePreview.TryGetSelfDamagePreviewAmount(skill, out int selfDmg) || selfDmg <= 0) return;

            bool immunePreview = _cheatController.Imortal;
            bool absorbed = !immunePreview && _hasNextHitShield;
            int effective = (!immunePreview && !absorbed) ? selfDmg : 0;
            int max = _naraConfiguration.MaxHealth;
            int actual = _naraData.ActualHealth;
            int baseline = _naraData.PreviewHealth;
            int projected = Mathf.Max(0, baseline - effective);
            _gamePlayUiController.BeginPlayerSelfDamageCastAimVisual(actual, baseline, projected, max);
        }

        public void EndSelfDamageCastAimPreview(bool cancel) {
            if (_gamePlayUiController == null) return;
            _gamePlayUiController.EndPlayerSelfDamageCastAimVisual(cancel, _naraData.ActualHealth, _naraConfiguration.MaxHealth);
        }

        public void TeleportToWorldPosition(Vector3 worldPosition) {
            if (_naraView == null) return;
            worldPosition = CombatGroundPositionSnap.SnapWorldPosition(worldPosition);
            var rb = _naraView.GetRigidbody();
            if (rb != null) {
                rb.position = worldPosition;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            _naraView.transform.position = worldPosition;
            Physics.SyncTransforms();
            if (_naraMovementController is NaraTurnMovementController ntm)
                ntm.RecenterMovementRingPreservingRadius();
        }

        #region IEffectable Methods

        public Transform GetReferenceTransform() {
            if (_naraView == null) return null;
            return _naraView.transform;
        }

        public void ResetPreview() => ResetSharedHealthPreview();

        public void ResetSharedHealthPreview() {
            _naraData.ResetPreview();
            _gamePlayUiController?.OnPreviewPlayerHealthUpdate(_naraData.PreviewHealth, _naraConfiguration.MaxHealth);
        }

        public void PreviewDamage(int damageAmound) {
            if (damageAmound <= 0) return;
            bool immunePreview = _cheatController.Imortal;
            bool absorbed = !immunePreview && _hasNextHitShield;
            int effectiveOnPreview = (!immunePreview && !absorbed) ? damageAmound : 0;
            ApplySharedHealthPreviewDamage(effectiveOnPreview);
        }

        public void ApplySharedHealthPreviewDamage(int amount) {
            _naraData.ApplyPreviewSubtractDamage(amount);
            _gamePlayUiController?.OnPreviewPlayerHealthUpdate(_naraData.PreviewHealth, _naraConfiguration.MaxHealth);
        }

        public void PreviewHeal(int healAmount) => ApplySharedHealthPreviewHeal(healAmount);

        public void ApplySharedHealthPreviewHeal(int healAmount) {
            _naraData.ApplyPreviewHeal(healAmount);
            _gamePlayUiController?.OnPreviewPlayerHealthUpdate(_naraData.PreviewHealth, _naraConfiguration.MaxHealth);
        }

        public void GrantNextHitShield() {
            _hasNextHitShield = true;
            _gamePlayUiController?.OnPlayerNextHitShieldChanged(true);
        }

        public bool HasNextHitShieldActive => _hasNextHitShield;

        public void GrantNextOutgoingDamageMultiplier(float multiplier)
        {
            _nextOutgoingDamageMultiplier = Mathf.Max(0f, multiplier);
            _hasNextOutgoingDamageModifier = true;
        }

        public bool HasNextOutgoingDamageMultiplier => _hasNextOutgoingDamageModifier;

        public float PendingNextOutgoingDamageMultiplier =>
            _hasNextOutgoingDamageModifier ? _nextOutgoingDamageMultiplier : 1f;

        public bool TryConsumeNextOutgoingDamageMultiplier(ref float multiplier)
        {
            if (!_hasNextOutgoingDamageModifier) return false;
            multiplier *= _nextOutgoingDamageMultiplier;
            _hasNextOutgoingDamageModifier = false;
            _nextOutgoingDamageMultiplier = 1f;
            return true;
        }

        public float OutgoingLifestealPercent => _outgoingLifestealPercent;

        public void SetOutgoingLifestealPercent(float percentOfDamageDealt) =>
            _outgoingLifestealPercent = Mathf.Max(0f, percentOfDamageDealt);

        public void TakeDamage(int damageAmound) {
            if (damageAmound <= 0) return;

            if (_cheatController.Imortal == false && _hasNextHitShield) {
                _hasNextHitShield = false;
                _gamePlayUiController?.OnPlayerNextHitShieldChanged(false);
                return;
            }

            ApplySharedHealthDamage(damageAmound, showNaraHitFeedback: true);
        }

        public void ApplySharedHealthDamage(int amount, bool showNaraHitFeedback) {
            if (amount <= 0) return;

            bool damageApplied = _cheatController.Imortal == false;
            if (damageApplied)
                _naraData.TakeDamage(amount);

            if (damageApplied && showNaraHitFeedback) {
                _damageStackMovementPassiveService?.OnPlayerDamageTaken();
                if (_naraView != null) {
                    var flash = _naraView.GetComponent<DamageFlashPresenter>();
                    if (flash == null) flash = _naraView.gameObject.AddComponent<DamageFlashPresenter>();
                    flash.TriggerFlash();
                }
                _audioService?.PlaySfx(SfxIds.Erza_Atingida, AudioChannelType.SfxCombat);
                _naraView?.PlayHitReaction();
                FloatingCombatNumberBridge.Show(_naraView != null ? _naraView.transform : null, amount, FloatingCombatNumberKind.Damage);
            }

            PushSharedHealthToHud();
            TryHandleSharedHealthDeath();
        }

        public void ApplySharedHealthHeal(int amount, bool showNaraHealFeedback) {
            if (amount <= 0) return;
            _naraData.Heal(amount);
            _naraData.ResetPreview();
            PushSharedHealthToHud();
            if (showNaraHealFeedback)
                FloatingCombatNumberBridge.Show(_naraView != null ? _naraView.transform : null, amount, FloatingCombatNumberKind.Heal);
        }

        void PushSharedHealthToHud() {
            _gamePlayUiController?.OnPlayerHealthUpdate(_naraData.ActualHealth, _naraConfiguration.MaxHealth);
            _gamePlayUiController?.OnPreviewPlayerHealthUpdate(_naraData.ActualHealth, _naraConfiguration.MaxHealth);
        }

        void TryHandleSharedHealthDeath() {
            if (!_naraData.IsAlive()) return;
            _audioService?.PlaySfx(SfxIds.Erza_Morte, AudioChannelType.SfxCombat);
            _naraView?.PlayDeath();
            _commandFactory.CreateCommandVoid<GameOverCommand>()
                .SetData(new GameOverCommandData(false, _naraView?.GetAnimator()))
                .Execute();
        }

        private void StartFootstepSfx() {
            if (_footstepsPlaying) return;
            _footstepsPlaying = true;
            _audioService?.SetSegmentLoopingSfx(SfxIds.Erza_Passos, AudioChannelType.SfxAmbience, FootstepLoopSegmentSeconds, true);
        }

        private void StopFootstepSfx() {
            if (!_footstepsPlaying) return;
            _footstepsPlaying = false;
            _audioService?.StopLoopingSfx(AudioChannelType.SfxAmbience);
        }

        public void PlayAttackType(int type, SkillCastAnimationStyle style = SkillCastAnimationStyle.Slow) {
            _naraView?.SetAttackType(type, style);
        }

        public void PlayAttackType1() {
            _naraView?.SetAttackType(1);
        }

        public void Heal(int healAmount) => ApplySharedHealthHeal(healAmount, showNaraHealFeedback: true);

        public void TriggerExecute() {
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
            if (value <= 0) return;
            var ap = EnsureApService();
            if (ap == null)
            {
                LakiTileEffectApplyDebug.LogManaSkipped("Player", "SubtractActionPoints", "IActionPointsService não resolvido");
                return;
            }
            int before = ap.Current;
            ap.Subtract(value);
            LakiTileEffectApplyDebug.LogManaDelta("Player", "SubtractActionPoints", before, ap.Current, -value);
        }

        public void SubtractAllActionPoints(int value) {

        }

        public void ReduceActionPointsGainPerTurn(int valueToSubtract, int duration) {

        }

        public void IncreaseActionPointsGainPerTurn(int valueToIncrease, int duration) {

        }

        public void AddActionPoints(int valueToIncrease) {
            if (valueToIncrease <= 0) return;
            var ap = EnsureApService();
            if (ap == null)
            {
                LakiTileEffectApplyDebug.LogManaSkipped("Player", "AddActionPoints", "IActionPointsService não resolvido");
                return;
            }
            int before = ap.Current;
            ap.Add(valueToIncrease);
            LakiTileEffectApplyDebug.LogManaDelta("Player", "AddActionPoints", before, ap.Current, valueToIncrease);
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
            _naraView?.ReleaseConjuring();
            if (_naraMovementController is NaraTurnMovementController ntm) {
                ntm.RecalculateRadiusAfterAbility();
                ntm.SetMovementRadiusCenter();
                ntm.Refresh();
            }
            Unfreeeze();
        }

        public void SetBookCloneDeployed(bool cloneDeployed) {
            if (cloneDeployed)
                _naraView?.PlayDivideDeployAnimation();
            else
                _naraView?.PlayDivideRecallAnimation();
            _naraView?.SetBookCloneDeployed(cloneDeployed);
        }

        public void BeginSkillGuidedDisplacementToWorldPosition(Vector3 worldTarget, float durationSeconds, Action onComplete) {
            if (_naraView == null) {
                onComplete?.Invoke();
                return;
            }

            Freeeze();

            var displacer = _naraView.GetComponent<ArenaSkillPathDisplacer>();
            if (displacer == null)
                displacer = _naraView.gameObject.AddComponent<ArenaSkillPathDisplacer>();

            Rigidbody rb = _naraView.GetRigidbody();
            displacer.Begin(rb, worldTarget, durationSeconds, () => {
                ArenaBoundedPlanarDisplacement.ZeroPlanarVelocity(rb);
                onComplete?.Invoke();
            });
        }

        #endregion
    }
}