using Logic.Scripts.GameDomain.MVC.Environment;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

/// <summary>
/// Teleports the caster to <see cref="SkillExecutionContext.TargetPoint"/> (aim / AoE marker).
/// For Projectile + <see cref="SkillDataSO.MoveCasterToProjectileHit"/>, does nothing here — arrival is handled only on projectile collision (same moment as damage).
/// On confirm, cast flow calls <see cref="IPlayableUnit.SyncArenaMovementAfterMovementSkillDisplacement"/> for <see cref="Logic.Scripts.GameDomain.Services.Skills.SkillType.Movement"/> skills (teleport keeps ring budget).
/// </summary>
[CreateAssetMenu(fileName = "TeleportCasterToAimEffect", menuName = "ScriptableObjects/Skills/Effects/TeleportCasterToAim")]
public class TeleportCasterToAimSkillEffectSO : SkillEffectSO
{
    [SerializeField] private bool _raycastDownToGround;
    [SerializeField] private float _groundRaycastHeight = 2f;
    [SerializeField] private float _groundRaycastDistance = 8f;
    [SerializeField] private LayerMask _groundLayers = ~0;

    public override void Execute(in SkillExecutionContext context)
    {
        if (context.Skill != null
            && context.Skill.CastType == SkillCastType.Projectile
            && context.Skill.MoveCasterToProjectileHit)
            return;

        if (context.Caster is not ISkillCasterWorldTeleport teleport) return;

        Vector3 destination = context.TargetPoint;
        if (_raycastDownToGround
            && Physics.Raycast(
                destination + Vector3.up * _groundRaycastHeight,
                Vector3.down,
                out RaycastHit hit,
                _groundRaycastDistance,
                _groundLayers,
                QueryTriggerInteraction.Ignore))
        {
            destination = hit.point;
        }
        else
        {
            destination = CombatGroundPositionSnap.SnapWorldPosition(destination);
        }

        CombatArenaBoundaryRuntime.TryClampVoluntaryWorldPosition(ref destination);
        destination = CombatGroundPositionSnap.SnapWorldPosition(destination);
        teleport.TeleportToWorldPosition(destination);
    }
}
