using Logic.Scripts.Core.Mvc.UICamera;
using Logic.Scripts.GameDomain.Commands;
using Logic.Scripts.GameDomain.MVC.Echo;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.States;
using Logic.Scripts.GameDomain.Utilities;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.StateMachineService;
using System.Threading;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.MVC.Ui {
    public class GamePlayUiController : IGamePlayUiController {
        private readonly IStateMachineService _stateMachineService;
        private readonly ExplorationState.Factory _explorationStateFactory;
        private readonly IUICameraController _uiCameraController;
        private readonly IAudioService _audioService;
        private readonly IGamePlayHudView _gamePlayHud;
        private readonly IPauseMenuView _pauseMenuView;
        private readonly IGameOverView _gameOverView;
        private readonly IUniversalUIController _universalUIController;
        private readonly ICommandFactory _commandFactory;
        private readonly ILevelsDataService _levelsDataService;
        private readonly IGamePlayDataService _gamePlayDataService;
        private readonly ICloneUseLimiter _cloneUseLimiter;
        private readonly ISkillVisualCatalog _skillVisualCatalog;

        public GamePlayUiController(IStateMachineService stateMachineService, ExplorationState.Factory explorationStateFactory,
            IUICameraController uiCameraController, IGamePlayHudView gamePlayHud, IAudioService audioService, IPauseMenuView pauseMenuView,
            IUniversalUIController universalUIController, ICommandFactory commandFactory, IGameOverView gameOverView,
            [InjectOptional] ILevelsDataService levelsDataService = null,
            [InjectOptional] IGamePlayDataService gamePlayDataService = null,
            [InjectOptional] ICloneUseLimiter cloneUseLimiter = null,
            [InjectOptional] ISkillVisualCatalog skillVisualCatalog = null) {
            _stateMachineService = stateMachineService;
            _explorationStateFactory = explorationStateFactory;
            _uiCameraController = uiCameraController;
            _gamePlayHud = gamePlayHud;
            _audioService = audioService;
            _pauseMenuView = pauseMenuView;
            _universalUIController = universalUIController;
            _commandFactory = commandFactory;
            _gameOverView = gameOverView;
            _levelsDataService = levelsDataService;
            _gamePlayDataService = gamePlayDataService;
            _cloneUseLimiter = cloneUseLimiter;
            _skillVisualCatalog = skillVisualCatalog;
        }

        public void InitEntryPoint() {
            _pauseMenuView.InitEntryPoint();
            _pauseMenuView.RegisterCallbacks(
                _universalUIController.ShowOptionsScreen,
                _universalUIController.ShowCreditsScreen,
                OnResumeFromPause,
                OnBackToLobby,
                OnQuitGame);
            _gamePlayHud.InitStartPoint();
            _gamePlayHud.RegisterCallbacks(OnClickNextTurn, OnClickAbility1, OnClickAbility2, OnClickAbility3, OnClickAbility4);
            _gamePlayHud.RegisterOpenPauseMenuCallback(OnOpenPauseMenu);
            SyncBossHudNameFromCurrentLevel();
            _gameOverView.InitEntryPoint();
            _gameOverView.RegisterCallbacks(OnClickPlayAgainWithSfx, OnBackToLobbyFromGameOver, OnQuitGameFromGameOver);
        }

        /// <summary>
        /// HUD boss title from <see cref="LevelTurnData"/>: per-level <c>bossHudDisplayName</c>, else <see cref="Logic.Scripts.GameDomain.MVC.Boss.BossConfigurationSO.BossDisplayName"/>.
        /// Matches the string bound into <see cref="Logic.Scripts.GameDomain.MVC.Boss.BossController"/> via <see cref="LoadLevelCommand.SetBoss"/>.
        /// </summary>
        private void SyncBossHudNameFromCurrentLevel() {
            if (_levelsDataService == null || _gamePlayDataService == null) return;
            int levelNumber = _gamePlayDataService.CurrentLevelNumber;
            if (_levelsDataService.GetLevelData(levelNumber) is not LevelTurnData turnData) return;
            string display = turnData.GetEffectiveBossHudDisplayName();
            if (string.IsNullOrWhiteSpace(display)) return;
            _gamePlayHud.OnBossDisplayNameChange(display.Trim());
        }
        #region GameplayUiInputs
        public void OnClickNextTurn() {
            _commandFactory.CreateCommandVoid<CompletePlayerActionCommand>().Execute();
        }

        private void OnOpenPauseMenu() =>
            _commandFactory.CreateCommandVoid<PauseGameplayInputCommand>().Execute();

        public void OnClickAbility1() {
            _commandFactory.CreateCommandVoid<UseAbility1InputCommand>().Execute();
        }

        public void OnClickAbility2() {
            _commandFactory.CreateCommandVoid<UseAbility2InputCommand>().Execute();
        }

        public void OnClickAbility3() {
            _commandFactory.CreateCommandVoid<UseAbility3InputCommand>().Execute();
        }

        public void OnClickAbility4() {
            _commandFactory.CreateCommandVoid<UseAbility4InputCommand>().Execute();
        }

        #endregion
        public void InitExitPoint() {

        }

        public Transform GameplayHudRoot() => _gamePlayHud.GetGameplayHudRoot();

        #region GameOver
        public void ShowGameOver(bool IsWin) {
            _gameOverView.Show(IsWin);
        }

        public async Awaitable ShowGameOverWithFadeAsync(bool isWin, float fadeDurationSeconds = 1f) {
            await _gameOverView.ShowWithFadeAsync(isWin, fadeDurationSeconds);
        }

        public async void OnClickPlayAgain() {
            GameOverCommand.ResetSequenceGuard();
            _commandFactory.CreateCommandVoid<ResumeGameplayInputCommand>().Execute();
            await _commandFactory.CreateCommandAsync<ReloadLevelCommand>().Execute(CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken));
            _gameOverView.Hide();
        }

        private void OnClickPlayAgainWithSfx() {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            OnClickPlayAgain();
        }
        public void OnLoad() {
            Debug.LogWarning("Clicou no load");
        }
        #endregion

        #region Pause
        public void ShowPauseScreen() => _pauseMenuView.Show();
        public void HidePauseScreen() => _pauseMenuView.Hide();

        private void OnQuitGame() {
            GeneralSfxFeedback.PlayMenuClick(_audioService, secondary: true);
            QuitApplicationUtility.Quit();
        }

        private void OnResumeFromPause() {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            ResumeGame();
        }

        private void ResumeGame() {
            _universalUIController.CloseAllOverlays();
            _commandFactory.CreateCommandVoid<ResumeGameplayInputCommand>().Execute();
        }

        private void OnBackToLobbyFromGameOver() {
            GeneralSfxFeedback.PlayMenuClick(_audioService, secondary: true);
            _gameOverView.Hide();
            BackToLobby();
        }

        private void OnQuitGameFromGameOver() {
            _gameOverView.Hide();
            OnQuitGame();
        }

        private void OnBackToLobby() {
            GeneralSfxFeedback.PlayMenuClick(_audioService, secondary: true);
            BackToLobby();
        }

        private void BackToLobby() {
            GameOverCommand.ResetSequenceGuard();
            _universalUIController.CloseAllOverlays();
            _commandFactory.CreateCommandVoid<ResumeGameplayInputCommand>().Execute();
            _stateMachineService.SwitchState(_explorationStateFactory.Create(new ExplorationInitiatorEnterData(0)));
        }
        #endregion

        public void SetPlayerValues(int previewHp, int actualHp, int maxHp) {
            _gamePlayHud.SnapPlayerHealth(previewHp, actualHp, maxHp);
        }

        public void SetAbilityManaCosts(int c1, int c2, int c3, int c4, bool showCostSlot1 = true, bool showCostSlot2 = true, bool showCostSlot3 = true, bool showCostSlot4 = true) {
            _gamePlayHud.SetAbilityManaCosts(c1, c2, c3, c4, showCostSlot1, showCostSlot2, showCostSlot3, showCostSlot4);
        }

        public void SetSkillHudIcons(Sprite erza0, Sprite erza1, Sprite erza2, Sprite erza3, Sprite book0, Sprite book1, Sprite book2, Sprite book3) =>
            _gamePlayHud.SetSkillHudIcons(erza0, erza1, erza2, erza3, book0, book1, book2, book3);

        public void SetSkillHudVisuals(
            SkillDataSO erza0, SkillDataSO erza1, SkillDataSO erza2, SkillDataSO erza3,
            SkillDataSO book0, SkillDataSO book1, SkillDataSO book2, SkillDataSO book3) =>
            _gamePlayHud.SetSkillHudVisuals(erza0, erza1, erza2, erza3, book0, book1, book2, book3, _skillVisualCatalog);

        public void OnBossDisplayNameChange(string displayName) => _gamePlayHud.OnBossDisplayNameChange(displayName);

        public void ShowWinPanel(CancellationTokenSource cancellationTokenSource) {

        }

        public void ShowGameOverPanel(CancellationTokenSource cancellationTokenSource) {

        }

        public void SnapBossHealth(int hp, int maxHp) => _gamePlayHud.SnapBossHealth(hp, maxHp);

        public void OnBossHealthUpdate(int hp, int maxHp) => _gamePlayHud.OnBossHealthUpdate(hp, maxHp);

        public void OnPreviewBossHealthChange(int newValue) => _gamePlayHud.OnPreviewBossHealthChange(newValue);

        public void OnPlayerHealthUpdate(int hp, int maxHp) => _gamePlayHud.OnPlayerHealthUpdate(hp, maxHp);

        public void OnPreviewPlayerHealthUpdate(int previewHp, int maxHp) => _gamePlayHud.OnPreviewPlayerHealthUpdate(previewHp, maxHp);

        public void SnapPlayerActionPoints(int current, int max) => _gamePlayHud.SnapPlayerActionPoints(current, max);

        public void OnPlayerActionPointsChange(int current, int max) => _gamePlayHud.OnPlayerActionPointsChange(current, max);

        public void OnPlayerNextHitShieldChanged(bool active) => _gamePlayHud.OnPlayerNextHitShieldChanged(active);

        public void BeginSkillCastAimPreview(IPlayableUnit caster, SkillDataSO skill, int apCost, bool showPlayerManaPreview, int apCurrent, int apMax) =>
            _gamePlayHud.BeginSkillCastAimPreview(caster, skill, apCost, showPlayerManaPreview, apCurrent, apMax);

        public void EndSkillCastAimPreviewCancel(IPlayableUnit caster) =>
            _gamePlayHud.EndSkillCastAimPreviewCancel(caster);

        public void EndSkillCastAimPreviewCommit(IPlayableUnit caster) =>
            _gamePlayHud.EndSkillCastAimPreviewCommit(caster);

        public void BeginPlayerSelfDamageCastAimVisual(int actualHp, int baselineHp, int projectedHpAfterSelfHit, int maxHp) =>
            _gamePlayHud.BeginPlayerSelfDamageCastAimVisual(actualHp, baselineHp, projectedHpAfterSelfHit, maxHp);

        public void EndPlayerSelfDamageCastAimVisual(bool cancel, int actualHp, int maxHp) =>
            _gamePlayHud.EndPlayerSelfDamageCastAimVisual(cancel, actualHp, maxHp);

        public void OnSkill1CostChange(int newValue) => _gamePlayHud.OnSkill1CostChange(newValue);

        public void OnSkill2CostChange(int newValue) => _gamePlayHud.OnSkill2CostChange(newValue);

        public void OnSkill3CostChange(int newValue) => _gamePlayHud.OnSkill3CostChange(newValue);

        public void OnSkill4CostChange(int newValue) => _gamePlayHud.OnSkill4CostChange(newValue);

        public void OnSkill1NameChange(string newValue) => _gamePlayHud.OnSkill1NameChange(newValue);

        public void OnSkill2NameChange(string newValue) => _gamePlayHud.OnSkill2NameChange(newValue);

        public void ShowBookSkillsTheme(bool showBookSkillsTheme) {
            _gamePlayHud.ShowBookSkillsTheme(showBookSkillsTheme);
            SyncBookCloneActionHud();
        }

        public void SyncBookCloneActionHud() {
            if (_cloneUseLimiter == null) return;
            _gamePlayHud.SetBookCloneActionAvailable(_cloneUseLimiter.CanUse());
        }

        public void SetSkillsSlidableExpanded(bool expanded, bool instant = false) =>
            _gamePlayHud.SetSkillsSlidableExpanded(expanded, instant);

        public void PlayPlayerTurnAnnouncement(int turnNumber) {
            GeneralSfxFeedback.PlayNewTurn(_audioService);
            _gamePlayHud.PlayPlayerTurnAnnouncement(turnNumber);
        }

        public void BeginFirstTurnPassTurnHint(int fightTurnNumber) =>
            _gamePlayHud.BeginFirstTurnPassTurnHint(fightTurnNumber);

        public void EndFirstTurnPassTurnHint() => _gamePlayHud.EndFirstTurnPassTurnHint();

    }
}
