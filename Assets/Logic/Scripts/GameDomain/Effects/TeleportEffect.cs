using System;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Environment;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

[Serializable]
public class TeleportEffect : AbilityEffect {
    [HideInInspector] public Vector3 _destination;

    public override void SetUp(Vector3 point) {
        base.SetUp(point);
        _destination = point;
    }

    public override void Execute(AbilityData data, IEffectable caster) {
        _destination = CombatGroundPositionSnap.SnapWorldPosition(_destination);
        if (caster is INaraController nara && nara.NaraMove is NaraTurnMovementController turnMovement) {
            turnMovement.RecalculateRadiusAfterAbility();
            int naraRadius = turnMovement.GetNaraRadius();
            turnMovement.RemoveMovementRadius();
            if (caster is ISkillCasterWorldTeleport worldTeleport)
                worldTeleport.TeleportToWorldPosition(_destination);
            else {
                nara.SetPosition(_destination);
                turnMovement.SetMovementRadiusCenter();
                turnMovement.Refresh();
            }
            turnMovement.SetNaraRadius(naraRadius);
            turnMovement.SetMovementRadiusCenter();
        }
        else if (caster is ISkillCasterWorldTeleport teleport) {
            teleport.TeleportToWorldPosition(_destination);
        }
        else if (caster != null) {
            caster.GetReferenceTransform().position = _destination;
        }

    }
}
