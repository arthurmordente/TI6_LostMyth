using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills.Debug
{
    public interface ISkillAttackHitboxDebugService
    {
        bool Enabled { get; set; }
        void SetPreview(SkillDataSO skill, IPlayableUnit playable, IEffectable caster);
        void ClearPreview();
        void RecordCommitted(SkillDataSO skill, IPlayableUnit playable, IEffectable caster, Transform target);
        void RecordProjectileCollider(SkillDivinity divinity, Collider collider);
        void DrawAllGizmos();
    }

    public sealed class SkillAttackHitboxDebugService : ISkillAttackHitboxDebugService
    {
        public static ISkillAttackHitboxDebugService Instance { get; private set; }

        const float CommittedLingerSeconds = 2.5f;

        readonly struct TimedShape
        {
            public readonly SkillAttackHitboxShape Shape;
            public readonly float ExpiresAt;

            public TimedShape(SkillAttackHitboxShape shape, float expiresAt)
            {
                Shape = shape;
                ExpiresAt = expiresAt;
            }
        }

        readonly List<TimedShape> _committed = new List<TimedShape>(8);
        readonly List<SkillAttackHitboxShape> _projectileColliders = new List<SkillAttackHitboxShape>(4);

        bool _hasPreview;
        SkillAttackHitboxShape _preview;
        int _projectileColliderFrame = -1;

        public bool Enabled { get; set; } = true;

        public SkillAttackHitboxDebugService()
        {
            Instance = this;
        }

        public void SetPreview(SkillDataSO skill, IPlayableUnit playable, IEffectable caster)
        {
            if (!Enabled) return;
            _hasPreview = SkillAttackHitboxGeometry.TryBuildPreview(skill, playable, caster, out _preview);
        }

        public void ClearPreview()
        {
            _hasPreview = false;
        }

        public void RecordCommitted(SkillDataSO skill, IPlayableUnit playable, IEffectable caster, Transform target)
        {
            if (!Enabled) return;
            if (!SkillAttackHitboxGeometry.TryBuildCommitted(skill, playable, caster, target, out SkillAttackHitboxShape shape))
                return;
            PruneExpired();
            _committed.Add(new TimedShape(shape, Time.time + CommittedLingerSeconds));
        }

        public void RecordProjectileCollider(SkillDivinity divinity, Collider collider)
        {
            if (!Enabled || collider == null) return;
            if (Time.frameCount != _projectileColliderFrame)
            {
                _projectileColliders.Clear();
                _projectileColliderFrame = Time.frameCount;
            }
            _projectileColliders.Add(SkillAttackHitboxShape.ColliderBounds(
                collider,
                SkillDivinityUtil.GetDebugHitboxColor(divinity)));
        }

        public void DrawAllGizmos()
        {
            if (!Enabled) return;
            PruneExpired();

            if (_hasPreview)
                SkillAttackHitboxGizmoDrawer.Draw(_preview);

            for (int i = 0; i < _committed.Count; i++)
                SkillAttackHitboxGizmoDrawer.Draw(_committed[i].Shape);

            for (int i = 0; i < _projectileColliders.Count; i++)
                SkillAttackHitboxGizmoDrawer.Draw(_projectileColliders[i]);
        }

        void PruneExpired()
        {
            float now = Time.time;
            for (int i = _committed.Count - 1; i >= 0; i--)
            {
                if (_committed[i].ExpiresAt <= now)
                    _committed.RemoveAt(i);
            }
        }
    }
}
