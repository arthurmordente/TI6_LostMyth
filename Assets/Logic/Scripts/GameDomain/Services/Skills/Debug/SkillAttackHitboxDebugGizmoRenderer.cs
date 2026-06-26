using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.Services.Skills.Debug
{
    [ExecuteAlways]
    public sealed class SkillAttackHitboxDebugGizmoRenderer : MonoBehaviour
    {
        ISkillAttackHitboxDebugService _service;

        [Inject]
        public void Construct(ISkillAttackHitboxDebugService service)
        {
            _service = service;
        }

        void OnDrawGizmos()
        {
            if (_service == null)
                _service = SkillAttackHitboxDebugService.Instance;
            _service?.DrawAllGizmos();
        }
    }

    public sealed class SkillAttackHitboxDebugBootstrap : IInitializable
    {
        readonly DiContainer _container;

        public SkillAttackHitboxDebugBootstrap(DiContainer container)
        {
            _container = container;
        }

        public void Initialize()
        {
            if (Object.FindAnyObjectByType<SkillAttackHitboxDebugGizmoRenderer>() != null)
                return;

            var go = new GameObject(nameof(SkillAttackHitboxDebugGizmoRenderer));
            Object.DontDestroyOnLoad(go);
            var renderer = go.AddComponent<SkillAttackHitboxDebugGizmoRenderer>();
            _container.Inject(renderer);
        }
    }
}
