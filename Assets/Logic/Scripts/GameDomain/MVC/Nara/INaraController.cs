using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Ui;
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
        void CreateNara(NaraMovementController movementController);
        void ResetController();
        void PlayAttackType1();
        void RegisterListeners();
        void UnregisterListeners();
        void ManagedFixedUpdate();
        void SetPosition(Vector3 movementCenter);
    }
}
