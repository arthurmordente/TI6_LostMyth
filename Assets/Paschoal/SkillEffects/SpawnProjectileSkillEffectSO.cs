using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

/// <summary>Spawns <see cref="SkillDataSO.AttackPrefab"/> toward <see cref="SkillExecutionContext.TargetPoint"/>; projectile reads damage/range from the skill.</summary>
[CreateAssetMenu(fileName = "SpawnProjectileEffect", menuName = "ScriptableObjects/Skills/Effects/SpawnProjectile")]
public class SpawnProjectileSkillEffectSO : SkillEffectSO
{
    public override void Execute(in SkillExecutionContext context)
    {
        SkillDataSO skill = context.Skill;
        if (skill == null || skill.AttackPrefab == null || context.Caster == null) return;

        Vector3 origin;
        if (context.Caster.GetTransformCastPoint() != null)
            origin = context.Caster.GetTransformCastPoint().position;
        else if (context.Caster.GetReferenceTransform() != null)
            origin = context.Caster.GetReferenceTransform().position;
        else
            origin = context.TargetPoint;

        Vector3 dir = context.TargetPoint - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-8f && context.Caster.GetReferenceTransform() != null)
            dir = Vector3.ProjectOnPlane(context.Caster.GetReferenceTransform().forward, Vector3.up);
        if (dir.sqrMagnitude < 1e-8f) dir = Vector3.forward;

        var instance = Object.Instantiate(skill.AttackPrefab, origin, Quaternion.LookRotation(dir.normalized, Vector3.up));
        var projectile = instance.GetComponent<Projectile>();
        projectile?.ConfigureForCast(skill);
    }
}
