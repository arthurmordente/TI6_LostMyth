using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Ui {
    public interface IGamePlayUiController {
        void InitEntryPoint();
        void ShowPauseScreen();
        void HidePauseScreen();
        void ShowGameOver(bool IsWin);
        Awaitable ShowGameOverWithFadeAsync(bool isWin, float fadeDurationSeconds = 1f);
        /// <summary>Root transform of the main fight HUD (uGUI).</summary>
        Transform GameplayHudRoot();
        /// <summary>Initial HUD sync (no tween).</summary>
        void SetPlayerValues(int previewHp, int actualHp, int maxHp);
        /// <summary>Updates the four skill-slot mana labels; hide flags omit passive slots (no mana UI).</summary>
        void SetAbilityManaCosts(int c1, int c2, int c3, int c4, bool showCostSlot1 = true, bool showCostSlot2 = true, bool showCostSlot3 = true, bool showCostSlot4 = true);

        /// <inheritdoc cref="IGamePlayHudView.SetSkillHudIcons"/>
        void SetSkillHudIcons(Sprite erza0, Sprite erza1, Sprite erza2, Sprite erza3, Sprite book0, Sprite book1, Sprite book2, Sprite book3);

        /// <inheritdoc cref="IGamePlayHudView.SetSkillHudVisuals"/>
        void SetSkillHudVisuals(
            SkillDataSO erza0, SkillDataSO erza1, SkillDataSO erza2, SkillDataSO erza3,
            SkillDataSO book0, SkillDataSO book1, SkillDataSO book2, SkillDataSO book3);

        void OnBossDisplayNameChange(string displayName);

        void SnapBossHealth(int hp, int maxHp);
        void OnBossHealthUpdate(int hp, int maxHp);
        void OnPreviewBossHealthChange(int percent0To100);

        void OnPlayerHealthUpdate(int hp, int maxHp);
        void OnPreviewPlayerHealthUpdate(int previewHp, int maxHp);

        void SnapPlayerActionPoints(int current, int max);
        void OnPlayerActionPointsChange(int current, int max);

        void OnPlayerNextHitShieldChanged(bool active);

        void BeginSkillCastAimPreview(IPlayableUnit caster, SkillDataSO skill, int apCost, bool showPlayerManaPreview, int apCurrent, int apMax);
        void EndSkillCastAimPreviewCancel(IPlayableUnit caster);
        void EndSkillCastAimPreviewCommit(IPlayableUnit caster);

        /// <inheritdoc cref="IGamePlayHudView.BeginPlayerSelfDamageCastAimVisual"/>
        void BeginPlayerSelfDamageCastAimVisual(int actualHp, int baselineHp, int projectedHpAfterSelfHit, int maxHp);

        /// <inheritdoc cref="IGamePlayHudView.EndPlayerSelfDamageCastAimVisual"/>
        void EndPlayerSelfDamageCastAimVisual(bool cancel, int actualHp, int maxHp);

        void OnSkill1CostChange(int newValue);

        void OnSkill2CostChange(int newValue);

        void OnSkill3CostChange(int newValue);

        void OnSkill4CostChange(int newValue);

        void OnSkill1NameChange(string newValue);

        void OnSkill2NameChange(string newValue);
        void ShowBookSkillsTheme(bool showBookSkillsTheme);

        /// <summary>Sincroniza o frasco 0/1 do clone com <see cref="Logic.Scripts.GameDomain.MVC.Echo.ICloneUseLimiter"/>.</summary>
        void SyncBookCloneActionHud();

        /// <summary>Sincroniza keybinds Divide / Join+Switch com estado do clone e uso do comando C no turno.</summary>
        void SyncDivideKeybindHud(bool cloneDeployed, bool divideCommandAvailable);

        /// <inheritdoc cref="IGamePlayHudView.SetSkillsSlidableExpanded"/>
        void SetSkillsSlidableExpanded(bool expanded, bool instant = false);

        /// <inheritdoc cref="IGamePlayHudView.PlayPlayerTurnAnnouncement"/>
        void PlayPlayerTurnAnnouncement(int turnNumber);

        /// <inheritdoc cref="IGamePlayHudView.BeginFirstTurnPassTurnHint"/>
        void BeginFirstTurnPassTurnHint(int fightTurnNumber);

        /// <inheritdoc cref="IGamePlayHudView.EndFirstTurnPassTurnHint"/>
        void EndFirstTurnPassTurnHint();

        /// <summary>Escurece slots sem recurso; tremor no slot + frasco de mana ao cast bloqueado.</summary>
        void RefreshSkillCastAffordability(int currentActionPoints, bool bookCloneAvailable);

        void PlayInsufficientCastFeedback(int slotIndex, CombatSkillCastBlockReason reason);
    }
}