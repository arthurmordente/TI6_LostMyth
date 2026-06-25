using Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills.Debug
{
    /// <summary>
    /// Builds hit-query geometry from <see cref="SkillDataSO"/> using the same math as preview and <see cref="SkillDataSO"/> target resolution.
    /// </summary>
    public static class SkillAttackHitboxGeometry
    {
        public const float SelfTargetGizmoRadius = 0.45f;

        public static bool TryBuildPreview(
            SkillDataSO skill,
            IPlayableUnit playable,
            IEffectable caster,
            out SkillAttackHitboxShape shape)
        {
            shape = default;
            if (skill == null || playable == null) return false;

            Color color = SkillDivinityUtil.GetDebugHitboxColor(skill.Divinity);
            switch (skill.CastType)
            {
                case SkillCastType.Area:
                {
                    float radius = skill.GetAreaRadius();
                    if (radius <= 0.0001f) return false;
                    Vector3 aim = NewSkillSystemSkillAimWorld.GetAreaClampedAimPoint(playable, caster, skill);
                    shape = SkillAttackHitboxShape.Sphere(aim, radius, color);
                    return true;
                }
                case SkillCastType.Projectile:
                {
                    Vector3 origin = NewSkillSystemSkillAimWorld.GetSkillOrigin(playable, caster);
                    Vector3 end = NewSkillSystemSkillAimWorld.GetPlanarClampedAimEnd(playable, caster, skill);
                    if ((end - origin).sqrMagnitude < 1e-8f) return false;
                    shape = SkillAttackHitboxShape.Segment(origin, end, color);
                    return true;
                }
                case SkillCastType.Self:
                {
                    Vector3 foot = SkillCastPresentationTarget.GetSelfCastFootWorld(playable, skill);
                    shape = SkillAttackHitboxShape.Sphere(foot, SelfTargetGizmoRadius, color);
                    return true;
                }
            }

            return false;
        }

        public static bool TryBuildCommitted(
            SkillDataSO skill,
            IPlayableUnit playable,
            IEffectable caster,
            Transform target,
            out SkillAttackHitboxShape shape)
        {
            shape = default;
            if (skill == null) return false;

            Color color = SkillDivinityUtil.GetDebugHitboxColor(skill.Divinity);
            if (skill.CastType == SkillCastType.Self)
            {
                IEffectable selfTarget = SkillCastRules.IsRangelessSelfBuff(skill)
                    ? SkillCastBeneficiaryResolver.TryResolve(skill, caster)
                    : caster;
                Transform reference = selfTarget?.GetReferenceTransform();
                Vector3 center = reference != null
                    ? reference.position
                    : target != null
                        ? target.position
                        : playable?.UnitViewGO != null
                            ? playable.UnitViewGO.transform.position
                            : Vector3.zero;
                shape = SkillAttackHitboxShape.Sphere(center, SelfTargetGizmoRadius, color);
                return true;
            }

            if (skill.CastType == SkillCastType.Area)
            {
                float radius = skill.GetAreaRadius();
                if (radius <= 0.0001f) return false;
                Vector3 center = target != null
                    ? target.position
                    : playable?.UnitViewGO != null
                        ? playable.UnitViewGO.transform.position
                        : Vector3.zero;
                shape = SkillAttackHitboxShape.Sphere(center, radius, color);
                return true;
            }

            if (skill.CastType == SkillCastType.Projectile && playable != null)
            {
                Vector3 origin = NewSkillSystemSkillAimWorld.GetSkillOrigin(playable, caster);
                Vector3 end = NewSkillSystemSkillAimWorld.GetPlanarClampedAimEnd(playable, caster, skill);
                if ((end - origin).sqrMagnitude < 1e-8f) return false;
                shape = SkillAttackHitboxShape.Segment(origin, end, color);
                return true;
            }

            return false;
        }
    }
}
