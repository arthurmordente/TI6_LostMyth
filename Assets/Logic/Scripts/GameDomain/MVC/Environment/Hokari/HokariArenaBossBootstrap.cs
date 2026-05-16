using Logic.Scripts.GameDomain.Commands;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Turns;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari
{
    /// <summary>
    /// Registers Hokari arena bounds, ring-out watcher, and environment displacement actor.
    /// Place in the Hokari boss fight scene (can share GameObject with ArenaPosReference).
    /// </summary>
    [ExecuteAlways]
    public sealed class HokariArenaBossBootstrap : MonoBehaviour
    {
        [Header("Debug gizmos (temporary — tuning)")]
        [SerializeField] private bool _drawArenaBoundsGizmo = true;
        [Tooltip("When Center World is zero, gizmos use this transform position.")]
        [SerializeField] private bool _useTransformPositionAsCenter;

        [Header("Arena footprint")]
        [SerializeField] private Vector3 _centerWorld;
        [SerializeField] private float _voluntaryClampRadius = 10f;
        [SerializeField] private float _ringOutFallY = -2f;
        [Tooltip("XZ radius beyond which consecutive frames trigger ring-out. 0 = Y-only.")]
        [SerializeField] private float _ringOutOutsideRadiusXZ = 11.5f;
        [SerializeField] private int _ringOutConsecutiveFrames = 3;

        [Header("Environment hazards")]
        [SerializeField] private HokariArenaHazardPatternSO _hazardPattern;

        CombatArenaEliminationWatcher _eliminationWatcher;

        void Start()
        {
            if (_centerWorld == Vector3.zero)
                _centerWorld = transform.position;

            CombatArenaBoundaryRuntime.RegisterHokari(new CombatArenaHokariGeometry
            {
                CenterWorld = _centerWorld,
                VoluntaryClampRadius = _voluntaryClampRadius,
                RingOutFallY = _ringOutFallY,
                RingOutOutsideRadiusXZ = _ringOutOutsideRadiusXZ,
                RingOutConsecutiveFrames = _ringOutConsecutiveFrames,
            });

            DiContainer container = ResolveSceneContainer(this);
            if (container == null)
            {
                Debug.LogError("[HokariArenaBossBootstrap] No Zenject SceneContext in this scene.");
                return;
            }

            TurnStateService turnState = null;
            INaraController nara = null;
            ICommandFactory commandFactory = null;
            try { turnState = container.Resolve<TurnStateService>(); } catch { }
            try { nara = container.Resolve<INaraController>(); } catch { }
            try { commandFactory = container.Resolve<ICommandFactory>(); } catch { }

            if (commandFactory != null)
            {
                try
                {
                    var updateService = container.Resolve<Logic.Scripts.Services.UpdateService.IUpdateSubscriptionService>();
                    _eliminationWatcher = new CombatArenaEliminationWatcher(updateService, commandFactory);
                    _eliminationWatcher.Register();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[HokariArenaBossBootstrap] Ring-out watcher not started: {ex.Message}");
                }
            }

            if (_hazardPattern != null && turnState != null && nara != null && commandFactory != null)
            {
                var actor = new HokariArenaDisplacementActor(turnState, nara, _hazardPattern, _centerWorld);
                var cmd = commandFactory.CreateCommandVoid<RegisterEnvironmentActorCommand>();
                cmd.SetActor(actor);
                cmd.Execute();
            }
            else if (_hazardPattern == null)
            {
                Debug.LogWarning("[HokariArenaBossBootstrap] No hazard pattern assigned — environment pushes disabled.");
            }
        }

        void OnDestroy()
        {
            _eliminationWatcher?.Unregister();
            CombatArenaBoundaryRuntime.Clear();
        }

        void OnValidate()
        {
            if (_useTransformPositionAsCenter)
                _centerWorld = transform.position;
#if UNITY_EDITOR
            if (_drawArenaBoundsGizmo)
                UnityEditor.SceneView.RepaintAll();
#endif
        }

        void OnDrawGizmosSelected()
        {
            if (!_drawArenaBoundsGizmo) return;
            Vector3 center = ResolveCenterForGizmos();
            CombatArenaBoundaryGizmoDrawer.DrawHokari(
                center,
                _voluntaryClampRadius,
                _ringOutFallY,
                _ringOutOutsideRadiusXZ);
        }

        void OnDrawGizmos()
        {
            if (!_drawArenaBoundsGizmo) return;
            Vector3 center = ResolveCenterForGizmos();
            CombatArenaBoundaryGizmoDrawer.DrawHokari(
                center,
                _voluntaryClampRadius,
                _ringOutFallY,
                _ringOutOutsideRadiusXZ);
        }

        Vector3 ResolveCenterForGizmos()
        {
            if (_useTransformPositionAsCenter || _centerWorld == Vector3.zero)
                return transform.position;
            return _centerWorld;
        }

        static DiContainer ResolveSceneContainer(MonoBehaviour host)
        {
            var sceneCtxs = Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneCtxs.Length; i++)
            {
                var sc = sceneCtxs[i];
                if (sc != null && sc.gameObject.scene == host.gameObject.scene)
                    return sc.Container;
            }
            return null;
        }
    }
}
