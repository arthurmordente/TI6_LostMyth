using Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

/// <summary>
/// Confirmed cast: instantiates <see cref="SkillDataSO.AttackPrefab"/> (visual + collider only is enough) and attaches
/// <see cref="SkillSpawnedProjectile"/> with all motion, range, pierce rules, damage and optional impact radius from the skill asset.
/// Direction matches preview: planar vector toward <see cref="SkillExecutionContext.TargetPoint"/>.
/// </summary>
[CreateAssetMenu(fileName = "SpawnProjectileEffect", menuName = "ScriptableObjects/Skills/Effects/SpawnProjectile")]
public class SpawnProjectileSkillEffectSO : SkillEffectSO
{
    public override void Execute(in SkillExecutionContext context)
    {
        SkillDataSO skill = context.Skill;
        if (skill == null || skill.AttackPrefab == null || context.Caster == null) return;

        Vector3 origin;
        if (context.Caster is IPlayableUnit playable)
            origin = NewSkillSystemSkillAimWorld.GetSkillOrigin(playable, context.Caster);
        else if (context.Caster.GetTransformCastPoint() != null)
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

        GameObject instance = Object.Instantiate(skill.AttackPrefab, origin, Quaternion.LookRotation(dir.normalized, Vector3.up));

        var motor = instance.GetComponent<SkillSpawnedProjectile>();
        if (motor == null)
            motor = instance.AddComponent<SkillSpawnedProjectile>();

        var args = new SkillProjectileSpawnArgs
        {
            Speed = skill.GetProjectileSpeed(),
            MaxRange = skill.GetProjectileRange(),
            MaxTargets = skill.GetProjectileMaxTargets(),
            HitMode = skill.GetProjectileHitMode(),
            Damage = skill.Power,
            ImpactAreaRadius = skill.GetProjectileImpactAreaRadius(),
            Caster = context.Caster
        };
        motor.Initialize(args);
    }
}
