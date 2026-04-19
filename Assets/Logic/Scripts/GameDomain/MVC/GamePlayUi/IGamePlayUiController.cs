using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Ui {
    public interface IGamePlayUiController {
        void InitEntryPoint();
        void ShowPauseScreen();
        void HidePauseScreen();
        void ShowGameOver(bool IsWin);
        /// <summary>Root transform of the main fight HUD (uGUI).</summary>
        Transform GameplayHudRoot();
        /// <summary>Initial HUD sync (no tween).</summary>
        void SetPlayerValues(int previewHp, int actualHp, int maxHp);
        /// <summary>Updates the four skill-slot mana labels from the active unit's ability set.</summary>
        void SetAbilityManaCosts(int c1, int c2, int c3, int c4);

        /// <inheritdoc cref="IGamePlayHudView.SetSkillHudIcons"/>
        void SetSkillHudIcons(Sprite erza0, Sprite erza1, Sprite erza2, Sprite erza3, Sprite book0, Sprite book1, Sprite book2, Sprite book3);

        void OnBossDisplayNameChange(string displayName);

        void SnapBossHealth(int hp, int maxHp);
        void OnBossHealthUpdate(int hp, int maxHp);
        void OnPreviewBossHealthChange(int percent0To100);

        void OnPlayerHealthUpdate(int hp, int maxHp);
        void OnPreviewPlayerHealthUpdate(int previewHp, int maxHp);

        void SnapPlayerActionPoints(int current, int max);
        void OnPlayerActionPointsChange(int current, int max);

        void OnSkill1CostChange(int newValue);

        void OnSkill2CostChange(int newValue);

        void OnSkill3CostChange(int newValue);

        void OnSkill4CostChange(int newValue);

        void OnSkill1NameChange(string newValue);

        void OnSkill2NameChange(string newValue);
        void ShowBookSkillsTheme(bool showBookSkillsTheme);

        /// <inheritdoc cref="IGamePlayHudView.SetSkillsSlidableExpanded"/>
        void SetSkillsSlidableExpanded(bool expanded, bool instant = false);

        /// <inheritdoc cref="IGamePlayHudView.PlayPlayerTurnAnnouncement"/>
        void PlayPlayerTurnAnnouncement(int turnNumber);

        /// <inheritdoc cref="IGamePlayHudView.BeginFirstTurnPassTurnHint"/>
        void BeginFirstTurnPassTurnHint(int fightTurnNumber);

        /// <inheritdoc cref="IGamePlayHudView.EndFirstTurnPassTurnHint"/>
        void EndFirstTurnPassTurnHint();
    }
}