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
    }
}