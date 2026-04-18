using System;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Ui
{
    /// <summary>Fight HUD (uGUI). Replaces the former UI Toolkit + UXML flow.</summary>
    public interface IGamePlayHudView
    {
        void InitStartPoint();

        void RegisterCallbacks(Action onNextTurn, Action onSkill1, Action onSkill2, Action onSkill3, Action onSkill4);

        /// <summary>Root of the main HUD (for optional anchoring / camera).</summary>
        Transform GetGameplayHudRoot();

        void OnBossDisplayNameChange(string displayName);

        void SnapBossHealth(int hp, int maxHp);
        void OnBossHealthUpdate(int hp, int maxHp);
        void OnPreviewBossHealthChange(int percent0To100);

        void SnapPlayerHealth(int previewHp, int actualHp, int maxHp);
        void OnPlayerHealthUpdate(int hp, int maxHp);
        void OnPreviewPlayerHealthUpdate(int previewHp, int maxHp);

        void SnapPlayerActionPoints(int current, int max);
        void OnPlayerActionPointsChange(int current, int max);

        void OnSkill1CostChange(int cost);
        void OnSkill2CostChange(int cost);
        void OnSkill3CostChange(int cost);
        void OnSkill4CostChange(int cost);
        void OnSkill1NameChange(string name);
        void OnSkill2NameChange(string name);
        void ShowBookSkillsTheme(bool showBookSkillsTheme);

        /// <summary>
        /// Barra de skills (<see cref="UiSlidableAnchoredPanel"/>). TurnFlow: abre após os gates no <c>PlayerAct</c>;
        /// fecha ao concluir / saltar o turno do jogador e nas outras fases. <paramref name="instant"/> ignora o tween.
        /// </summary>
        void SetSkillsSlidableExpanded(bool expanded, bool instant = false);
    }
}
