using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Environment.Hokari;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.AttackGizmos
{
    public sealed class BossAttackDebugGizmoService : IBossAttackDebugGizmoService
    {
        static BossAttackDebugGizmoService _instance;
        public static BossAttackDebugGizmoService Instance => _instance;

        readonly List<BossAttackDebugShape> _shapes = new List<BossAttackDebugShape>(8);

        public BossAttackDebugGizmoService()
        {
            _instance = this;
        }

        public void SetActiveShapes(IReadOnlyList<BossAttackDebugShape> shapes)
        {
            _shapes.Clear();
            if (shapes == null) return;
            for (int i = 0; i < shapes.Count; i++)
                _shapes.Add(shapes[i]);
            AppendEnvironmentHazardShapes();
        }

        public void DrawAllGizmos()
        {
            for (int i = 0; i < _shapes.Count; i++)
                BossAttackGizmoDrawer.Draw(_shapes[i]);
        }

        void AppendEnvironmentHazardShapes()
        {
            if (!HokariArenaHazardRuntimeSchedule.TryGetActiveCommit(
                    out int executionTurn,
                    out Vector3 anchor,
                    out float discRadius))
                return;

            _shapes.Add(new BossAttackDebugShape
            {
                Kind = BossAttackDebugShapeKind.Disc,
                Origin = anchor,
                Radius = Mathf.Max(0.1f, discRadius),
                Color = new Color(0.2f, 0.9f, 1f, 0.85f),
                Label = $"HokariHazard T{executionTurn}",
            });
        }
    }
}
