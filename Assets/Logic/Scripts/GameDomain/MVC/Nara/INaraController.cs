using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Turns;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara {
    /// <summary>
    /// Nara-specific interface. Extends IPlayableUnit so Nara can be used
    /// interchangeably with the Book in the active-unit system.
    /// </summary>
    public interface INaraController : IPlayableUnit {
        GameObject NaraViewGO { get; }
        Transform NaraSkillSpotTransform { get; }
        NaraMovementController NaraMove { get; }
        void InitEntryPointExploration();
        void InitEntryPointGamePlay(IGamePlayUiController gamePlayUiController);
        /// <summary>Once per fight after <see cref="InitEntryPointGamePlay"/>: passive skills + <see cref="NaraConfigurationSO"/> mana gain/max.</summary>
        void ApplyCombatLoadoutPassivesAndActionPoints(IActionPointsService actionPoints);
        void CreateNara(NaraMovementController movementController);
        void ResetController();
        void PlayAttackType1();
        void RegisterListeners();
        void UnregisterListeners();
        void ManagedFixedUpdate();
        void SetPosition(Vector3 movementCenter);

        /// <summary>During skill aim: if the skill has self-damage, dip main HP bar toward post-hit HP; preview stays at heal baseline.</summary>
        void BeginSelfDamageCastAimPreviewFromSkill(SkillDataSO skill);

        /// <summary>End self-damage aim visual. Call before <see cref="Logic.Scripts.GameDomain.Effects.IEffectable.ResetPreview"/> on cancel/commit.</summary>
        void EndSelfDamageCastAimPreview(bool cancel);

        /// <summary>Swap player Animator between Erz+Book (false) and Erzahler solo (true) when Divide deploys the Book clone.</summary>
        void SetBookCloneDeployed(bool cloneDeployed);

        int CurrentHealth { get; }
        int MaxHealth { get; }

        /// <summary>Book clone and Nara share one HP pool (NaraData). Book passes showNaraHitFeedback=false.</summary>
        void ApplySharedHealthDamage(int amount, bool showNaraHitFeedback);
        void ApplySharedHealthHeal(int amount, bool showNaraHealFeedback);
        void ApplySharedHealthPreviewDamage(int amount);
        void ApplySharedHealthPreviewHeal(int amount);
        void ResetSharedHealthPreview();
    }
}
