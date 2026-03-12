using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.Turns;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Shared
{
    /// <summary>
    /// Common interface for any unit that the player can directly control (Nara, Book).
    /// Extends IEffectable so abilities can target/cast from it, and IEffectableAction
    /// so effects (stun, AP modifications, etc.) can be applied uniformly.
    /// </summary>
    public interface IPlayableUnit : IEffectable, IEffectableAction
    {
        GameObject UnitViewGO { get; }
        Transform UnitSkillSpotTransform { get; }

        void Freeeze();
        void Unfreeeze();
        void FreezeInputs();
        void UnfreezeInputs();
        void StopMovingAnim();

        void PlayAttackType(int type);
        void TriggerExecute();
        void ResetExecuteTrigger();
        void TriggerCancel();

        /// <summary>
        /// Enables or disables the movement response for this unit without touching the
        /// Input Action Map (which is shared between Nara and Book).
        /// Use this when switching active control via TAB so only the active unit moves.
        /// </summary>
        void SetMovementActive(bool isActive);

        /// <summary>Returns this unit's own Action Points service. Null means no AP cost (free cast).</summary>
        IActionPointsService GetActionPoints();

        /// <summary>
        /// Returns the ability set that belongs to this unit.
        /// Nara and Book can each hold different arrays, configured independently in the inspector.
        /// CastController always casts from the active unit's own set.
        /// </summary>
        AbilityData[] GetAbilities();

        /// <summary>Called right after this unit executes an ability, so it can update its movement area.</summary>
        void OnAbilityExecuted();

        /// <summary>
        /// Called when this unit becomes the actively controlled unit (TAB switch or turn start).
        /// Implementations should show their movement-range line indicator.
        /// </summary>
        void OnBecomeActive();

        /// <summary>
        /// Called when this unit loses active control (TAB switch, turn end, or recall).
        /// Implementations should hide their movement-range line indicator.
        /// </summary>
        void OnBecomeInactive();
    }
}
