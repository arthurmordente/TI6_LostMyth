using Logic.Scripts.GameDomain.MVC.Ui;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara {
    public interface INaraController {
        GameObject NaraViewGO { get; }
        Transform NaraSkillSpotTransform { get; }
        NaraMovementController NaraMove { get; }
        void InitEntryPointExploration();
        void InitEntryPointGamePlay(IGamePlayUiController gamePlayUiController);
        void CreateNara(NaraMovementController movementController);
        void ResetController();
        void StopMovingAnim();
        void Freeeze();
        void Unfreeeze();
        void FreezeInputs();
        void UnfreezeInputs();
        void RegisterListeners();
        void UnregisterListeners();
        void ManagedFixedUpdate();
        void PlayAttackType(int type);
        void PlayAttackType1();
        void TriggerExecute();
        void ResetExecuteTrigger();
        void TriggerCancel();
        void SetPosition(Vector3 movementCenter);
    }
}
