using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.Services.UpdateService;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Cast.NewSkillSystem {
    public class NewSkillSystemSkillTargetingPreviewService : INewSkillSystemSkillTargetingPreviewService, IUpdatable {
        private readonly IUpdateSubscriptionService _subscriptionService;

        private bool _registered;
        private SkillDataSO _skill;
        private IPlayableUnit _playable;
        private IEffectable _caster;
        private Transform _aimVisualRoot;

        private readonly HashSet<IEffectable> _highlighted = new HashSet<IEffectable>();

        public NewSkillSystemSkillTargetingPreviewService(IUpdateSubscriptionService subscriptionService) {
            _subscriptionService = subscriptionService;
        }

        public void Begin(SkillDataSO skill, IPlayableUnit playableCaster, Transform aoeVisualRoot = null) {
            End();
            if (skill == null || playableCaster == null) return;
            if (NewSkillSystemSkillTargetingRules.GetHighlightKind(skill) == NewSkillSystemAimHighlightKind.None) return;

            _skill = skill;
            _playable = playableCaster;
            _caster = playableCaster;
            _aimVisualRoot = aoeVisualRoot;
            _subscriptionService.RegisterUpdatable(this);
            _registered = true;
        }

        public void End() {
            if (!_registered) return;
            ClearAllHighlights();
            _subscriptionService.UnregisterUpdatable(this);
            _registered = false;
            _skill = null;
            _playable = null;
            _caster = null;
            _aimVisualRoot = null;
        }

        public void ManagedUpdate() {
            if (!_registered || _skill == null || _playable == null) return;

            var next = new HashSet<IEffectable>();
            switch (NewSkillSystemSkillTargetingRules.GetHighlightKind(_skill)) {
                case NewSkillSystemAimHighlightKind.GroundAreaSphere:
                    SyncAoeVisualRoot();
                    CollectAreaTargets(next);
                    break;
                case NewSkillSystemAimHighlightKind.DirectedLine:
                    SyncDirectedAimVisualRoot();
                    CollectDirectedLineTargets(next);
                    break;
            }
            ApplyHighlightDiff(next);
        }

        private void SyncAoeVisualRoot() {
            if (_aimVisualRoot == null || _skill == null || _playable == null) return;
            Vector3 aim = NewSkillSystemSkillAimWorld.GetAreaClampedAimPoint(_playable, _caster, _skill);
            Vector3 origin = NewSkillSystemSkillAimWorld.GetSkillOrigin(_playable, _caster);
            Vector3 direction = aim - origin;
            _aimVisualRoot.position = aim;
            if (direction.sqrMagnitude > 0.0001f)
                _aimVisualRoot.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float baseR = _skill.AoEPrefabBaseRadius <= 0f ? 1f : _skill.AoEPrefabBaseRadius;
            float uniform = _skill.GetAreaRadius() / Mathf.Max(0.01f, baseR);
            _aimVisualRoot.localScale = new Vector3(uniform, uniform, uniform);
        }

        /// <summary>
        /// Stretches the projectile aim prefab along the planar cast segment (origin → clamped aim end). Scale.z = segmentLength / base length.
        /// </summary>
        private void SyncDirectedAimVisualRoot() {
            if (_aimVisualRoot == null || _skill == null || _playable == null) return;
            Vector3 origin = NewSkillSystemSkillAimWorld.GetSkillOrigin(_playable, _caster);
            Vector3 end = NewSkillSystemSkillAimWorld.GetPlanarClampedAimEnd(_playable, _caster, _skill);
            Vector3 dir = end - origin;
            float len = dir.magnitude;
            _aimVisualRoot.position = origin;
            if (len > 0.0001f)
                _aimVisualRoot.rotation = Quaternion.LookRotation(dir / len, Vector3.up);
            else {
                Vector3 fallback = NewSkillSystemSkillAimWorld.GetPlanarDirectionFromOriginToAim(_playable, _caster);
                _aimVisualRoot.rotation = Quaternion.LookRotation(fallback, Vector3.up);
            }
            float baseLen = _skill.ProjectileAimPreviewBaseLength <= 0f ? 1f : _skill.ProjectileAimPreviewBaseLength;
            float zScale = len > 0.0001f ? len / Mathf.Max(0.01f, baseLen) : 0.01f;
            _aimVisualRoot.localScale = new Vector3(1f, 1f, zScale);
        }

        private void CollectAreaTargets(HashSet<IEffectable> next) {
            Vector3 aim = NewSkillSystemSkillAimWorld.GetAreaClampedAimPoint(_playable, _caster, _skill);
            var hits = Physics.OverlapSphere(aim, _skill.GetAreaRadius(), ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
                TryAddCombatTarget(hits[i], next);
        }

        private void CollectDirectedLineTargets(HashSet<IEffectable> next) {
            Vector3 origin = NewSkillSystemSkillAimWorld.GetSkillOrigin(_playable, _caster);
            Vector3 end = NewSkillSystemSkillAimWorld.GetPlanarClampedAimEnd(_playable, _caster, _skill);
            Vector3 dir = end - origin;
            float segLen = dir.magnitude;
            if (segLen < 1e-5f) return;
            Vector3 dirN = dir / segLen;

            bool pierce = NewSkillSystemSkillTargetingRules.GetDirectedLineUsesPierce(_skill);
            int maxTargets = NewSkillSystemSkillTargetingRules.GetDirectedLineMaxTargets(_skill);
            var rayHits = Physics.RaycastAll(origin, dirN, segLen, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(rayHits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < rayHits.Length; i++) {
                if (!TryResolveCombatTarget(rayHits[i].collider, out IEffectable effectable)) continue;
                next.Add(effectable);
                if (!pierce) break;
                if (next.Count >= maxTargets) break;
            }
        }

        private void TryAddCombatTarget(Collider col, HashSet<IEffectable> next) {
            if (TryResolveCombatTarget(col, out IEffectable e))
                next.Add(e);
        }

        private bool TryResolveCombatTarget(Collider col, out IEffectable effectable) {
            effectable = null;
            if (col == null) return false;
            effectable = col.GetComponentInParent<IEffectable>();
            if (effectable == null) return false;
            if (ReferenceEquals(effectable, _caster)) return false;
            // Player / Book: not highlighted while aiming new skill system skills. (Highlighting the player inside allied AoE etc. can use the same IEffectable API later.)
            if (effectable is IPlayableUnit) return false;
            if (LakiBossShieldRuntime.ShouldSuppressNewSkillSystemHighlightFor(effectable)) return false;
            return true;
        }

        private void ApplyHighlightDiff(HashSet<IEffectable> next) {
            _highlighted.RemoveWhere(e => {
                if (e == null || !next.Contains(e)) {
                    e?.SetSkillTargetingHighlight(false);
                    return true;
                }
                return false;
            });

            foreach (var e in next) {
                if (e == null) continue;
                if (_highlighted.Add(e))
                    e.SetSkillTargetingHighlight(true);
            }
        }

        private void ClearAllHighlights() {
            foreach (var e in _highlighted)
                e?.SetSkillTargetingHighlight(false);
            _highlighted.Clear();
        }
    }
}
