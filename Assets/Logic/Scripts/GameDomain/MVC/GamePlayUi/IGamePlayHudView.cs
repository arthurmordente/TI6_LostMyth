using System;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Ui
{
    /// <summary>Fight HUD (uGUI). Replaces the former UI Toolkit + UXML flow.</summary>
    public interface IGamePlayHudView
    {
        void InitStartPoint();

        void RegisterCallbacks(Action onNextTurn, Action onSkill1, Action onSkill2, Action onSkill3, Action onSkill4);

        /// <summary>Opcional. Mesmo fluxo que ESC no gameplay (PauseGameplayInputCommand).</summary>
        void RegisterOpenPauseMenuCallback(Action onOpenPauseMenu);

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

        void OnPlayerNextHitShieldChanged(bool active);

        void BeginSkillCastAimPreview(IPlayableUnit caster, SkillDataSO skill, int apCost, bool showPlayerManaPreview, int apCurrent, int apMax);
        void EndSkillCastAimPreviewCancel(IPlayableUnit caster);
        void EndSkillCastAimPreviewCommit(IPlayableUnit caster);

        /// <summary>
        /// During Nara aim with self-damage: preview layer holds <paramref name="baselineHp"/> (e.g. after heal preview);
        /// main HP fill tweens toward <paramref name="projectedHpAfterSelfHit"/>.
        /// </summary>
        void BeginPlayerSelfDamageCastAimVisual(int actualHp, int baselineHp, int projectedHpAfterSelfHit, int maxHp);

        /// <summary>
        /// <paramref name="cancel"/> true: tween main fill back to <paramref name="actualHp"/>; false: snap main to actual before cast resolves.
        /// </summary>
        void EndPlayerSelfDamageCastAimVisual(bool cancel, int actualHp, int maxHp);

        void OnSkill1CostChange(int cost);
        void OnSkill2CostChange(int cost);
        void OnSkill3CostChange(int cost);
        void OnSkill4CostChange(int cost);

        /// <summary>Nara HUD: per-slot mana label + optional root (see cost display lists on the view).</summary>
        void SetAbilityManaCosts(int c1, int c2, int c3, int c4, bool showCostSlot1 = true, bool showCostSlot2 = true, bool showCostSlot3 = true, bool showCostSlot4 = true);
        void OnSkill1NameChange(string name);
        void OnSkill2NameChange(string name);
        void ShowBookSkillsTheme(bool showBookSkillsTheme);

        /// <summary>Livro: custo universal (um uso/turno) — fill 0–1 animado + texto opcional 0/1.</summary>
        void SetBookCloneActionAvailable(bool available);

        /// <summary>Keybind C: Divide quando sem clone; Join+Switch quando clone ativo. Ofusca Divide/Join se C já usado no turno.</summary>
        void SetDivideKeybindState(bool cloneDeployed, bool divideCommandAvailable);

        /// <summary>Ícones dos 4 slots no HUD (sprites vindos de <c>SkillDataSO.Icon</c>). Null limpa o slot.</summary>
        void SetSkillHudIcons(Sprite erza0, Sprite erza1, Sprite erza2, Sprite erza3, Sprite book0, Sprite book1, Sprite book2, Sprite book3);

        /// <summary>Paint + frame + ícone por slot a partir do loadout e <see cref="ISkillVisualCatalog"/>.</summary>
        void SetSkillHudVisuals(
            SkillDataSO erza0, SkillDataSO erza1, SkillDataSO erza2, SkillDataSO erza3,
            SkillDataSO book0, SkillDataSO book1, SkillDataSO book2, SkillDataSO book3,
            ISkillVisualCatalog visualCatalog);

        /// <summary>
        /// Barra de skills (<see cref="UiSlidableAnchoredPanel"/>). TurnFlow: abre após os gates no <c>PlayerAct</c>;
        /// fecha ao concluir / saltar o turno do jogador e nas outras fases. <paramref name="instant"/> ignora o tween.
        /// </summary>
        void SetSkillsSlidableExpanded(bool expanded, bool instant = false);

        /// <summary>Anúncio “Turno N” no centro: abre (alpha 0→1 + escala), mantém, fecha (alpha 1→0 + escala).</summary>
        void PlayPlayerTurnAnnouncement(int turnNumber);

        /// <summary>Só <paramref name="fightTurnNumber"/> == 1: após delay, anima o botão passar turno (mesma lógica que DicePromptUI) até <see cref="EndFirstTurnPassTurnHint"/>.</summary>
        void BeginFirstTurnPassTurnHint(int fightTurnNumber);

        void EndFirstTurnPassTurnHint();

        /// <summary>Alpha dos slots activos conforme mana (Erza) ou cast único (Livro).</summary>
        void RefreshSkillCastAffordability(int currentActionPoints, bool bookCloneAvailable);

        /// <summary>Tremor no slot + frasco de mana quando o cast é rejeitado por falta de recurso.</summary>
        void PlayInsufficientCastFeedback(int slotIndex, CombatSkillCastBlockReason reason);
    }
}
