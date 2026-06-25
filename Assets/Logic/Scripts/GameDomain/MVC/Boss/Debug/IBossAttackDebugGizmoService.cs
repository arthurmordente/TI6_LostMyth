using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.MVC.Boss.AttackGizmos
{
    public interface IBossAttackDebugGizmoService
    {
        void SetActiveShapes(IReadOnlyList<BossAttackDebugShape> shapes);
        void DrawAllGizmos();
    }
}
