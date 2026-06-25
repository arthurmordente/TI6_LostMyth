using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.MVC.Boss.AttackGizmos
{
    [ExecuteAlways]
    public sealed class BossAttackDebugGizmoRenderer : MonoBehaviour
    {
        IBossAttackDebugGizmoService _service;

        [Inject]
        public void Construct(IBossAttackDebugGizmoService service)
        {
            _service = service;
        }

        void OnDrawGizmos()
        {
            if (_service == null)
                _service = BossAttackDebugGizmoService.Instance;
            _service?.DrawAllGizmos();
        }
    }

    public sealed class BossAttackDebugGizmoBootstrap : IInitializable
    {
        readonly DiContainer _container;

        public BossAttackDebugGizmoBootstrap(DiContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
            if (Object.FindAnyObjectByType<BossAttackDebugGizmoRenderer>() != null)
                return;

            var go = new GameObject(nameof(BossAttackDebugGizmoRenderer));
            Object.DontDestroyOnLoad(go);
            var renderer = go.AddComponent<BossAttackDebugGizmoRenderer>();
            _container.Inject(renderer);
        }
    }
}
