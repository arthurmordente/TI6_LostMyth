using Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem;
using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Instantiates <see cref="SkillDataSO.ProjectilePrefab"/> and attaches <see cref="SkillSpawnedProjectile"/>.
    /// Lives in Logic so <see cref="SkillEffectsRunner"/> and any assembly boundary do not depend on Paschoal effect assets.
    /// </summary>
    public static class SkillProjectileSpawn
    {
        public static void ExecuteSpawn(in SkillExecutionContext context)
        {
            SkillDataSO skill = context.Skill;
            if (skill == null || skill.ProjectilePrefab == null || context.Caster == null) return;

            var playableCaster = context.Caster as IPlayableUnit;

            Vector3 origin;
            if (playableCaster != null)
                origin = NewSkillSystemSkillAimWorld.GetSkillOrigin(playableCaster, context.Caster);
            else if (context.Caster.GetTransformCastPoint() != null)
                origin = context.Caster.GetTransformCastPoint().position;
            else if (context.Caster.GetReferenceTransform() != null)
                origin = context.Caster.GetReferenceTransform().position;
            else
                origin = context.TargetPoint;

            Vector3 dir;
            if (playableCaster != null && skill.CastType == SkillCastType.Projectile)
                dir = NewSkillSystemSkillAimWorld.GetPlanarDirectionFromOriginToAim(playableCaster, context.Caster);
            else
            {
                dir = context.TargetPoint - origin;
                dir.y = 0f;
            }
            if (dir.sqrMagnitude < 1e-8f && context.Caster.GetReferenceTransform() != null)
                dir = Vector3.ProjectOnPlane(context.Caster.GetReferenceTransform().forward, Vector3.up);
            if (dir.sqrMagnitude < 1e-8f) dir = Vector3.forward;
            Vector3 dirN = dir.normalized;
            origin += dirN * skill.ProjectileSpawnForwardOffset;

            GameObject instance = Object.Instantiate(skill.ProjectilePrefab, origin, Quaternion.LookRotation(dirN, Vector3.up));

            var motor = instance.GetComponent<SkillSpawnedProjectile>();
            if (motor == null)
                motor = instance.AddComponent<SkillSpawnedProjectile>();

            var args = new SkillProjectileSpawnArgs
            {
                Speed = skill.GetProjectileSpeed(),
                MaxRange = skill.GetProjectileRange(),
                MaxTargets = skill.GetProjectileMaxTargets(),
                Damage = skill.GetProjectileCollisionDamage(),
                Caster = context.Caster,
                MoveCasterToHit = skill.MoveCasterToProjectileHit,
                PullStandoffFromTarget = skill.ProjectilePullStandoffFromTargetMeters,
                MinTravelBeforeHitMeters = skill.ProjectileMinTravelBeforeHitMeters,
                HitDisplacementDurationSeconds = skill.ProjectileHitDisplacementDurationSeconds
            };
            motor.Initialize(args);
        }
    }

    /// <summary>
    /// Implemented by spawn-projectile <see cref="SkillEffectSO"/> assets. <see cref="SkillEffectsRunner"/> skips these when
    /// <see cref="DeclarativeSkillDataSO"/> already performed built-in projectile spawn.
    /// </summary>
    public interface ISpawnProjectileSkillEffect { }
}
