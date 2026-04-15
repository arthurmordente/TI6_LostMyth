using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core;
using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Logic.Scripts.GameDomain.MVC.Boss.Attacks.Circle
{
    public sealed class PlayerFootCircleAttackHandler : IBossAttackHandler, ITelegraphVisibility
    {
        private readonly float _radius;
        private readonly float _ringWidth;
        private readonly Material _lineMaterial;
        private readonly Material _meshMaterial;

        private LineRenderer _ring;
        private MeshFilter _discFilter;
        private MeshRenderer _discRenderer;
        private float _yOffset = 0.05f;
        private int _rqAdd;

        public PlayerFootCircleAttackHandler(float radius, float ringWidth, Material lineMaterial, Material meshMaterial)
        {
            _radius = Mathf.Max(0.1f, radius);
            _ringWidth = Mathf.Max(0.02f, ringWidth);
            _lineMaterial = lineMaterial;
            _meshMaterial = meshMaterial;
        }

        public void PrepareTelegraph(Transform parentTransform)
        {
            var center = ResolvePlayerWorldPosition();
            var layering = Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphLayeringLocator.Service;
            var layer = layering != null ? layering.Register(preferTop: false) : default;
            _yOffset = layer.Y;
            _rqAdd = layer.QueueAdd;

            var ringGo = new GameObject("PlayerFootCircle_Ring");
            ringGo.transform.SetParent(parentTransform, false);
            _ring = ringGo.AddComponent<LineRenderer>();
            var ringMat = _lineMaterial != null ? new Material(_lineMaterial) : new Material(Shader.Find("Sprites/Default"));
            ringMat.renderQueue += _rqAdd;
            _ring.material = ringMat;
            _ring.useWorldSpace = true;
            _ring.loop = true;
            _ring.widthMultiplier = _ringWidth;
            DrawCircle(center, _radius, 64);

            var discGo = new GameObject("PlayerFootCircle_Fill");
            discGo.transform.SetParent(parentTransform, false);
            _discFilter = discGo.AddComponent<MeshFilter>();
            _discRenderer = discGo.AddComponent<MeshRenderer>();
            var discMat = _meshMaterial != null ? new Material(_meshMaterial) : new Material(Shader.Find("Sprites/Default"));
            discMat.renderQueue += _rqAdd;
            _discRenderer.material = discMat;
            var effectiveRadius = Mathf.Max(0.01f, _radius - _ringWidth * 0.5f);
            _discFilter.sharedMesh = BuildFilledDisc(effectiveRadius, 64);
            _discFilter.transform.position = new Vector3(center.x, _yOffset, center.z);

            SetTelegraphVisible(false);
            Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.Register(this);
        }

        public bool ComputeHits(ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
        {
            if (arenaReference == null) return false;
            var playerWorld = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
            var center = ResolvePlayerWorldPosition();
            center.y = playerWorld.y;
            var dist = Vector3.Distance(playerWorld, center);
            return dist <= _radius + 1e-4f;
        }

        public System.Collections.IEnumerator ExecuteEffects(List<AbilityEffect> effects, ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
        {
            if (effects == null || effects.Count == 0 || arenaReference == null) yield break;
            var target = arenaReference.NaraController as IEffectable;
            if (target == null) yield break;

            for (var i = 0; i < effects.Count; i++)
            {
                var fx = effects[i];
                if (fx == null) continue;
                if (fx is IAsyncEffect asyncFx) yield return asyncFx.ExecuteRoutine(caster, target);
                else fx.Execute(caster, target);
            }
        }

        public void Cleanup()
        {
            if (_ring != null) { Object.Destroy(_ring.gameObject); _ring = null; }
            if (_discFilter != null) { Object.Destroy(_discFilter.gameObject); _discFilter = null; _discRenderer = null; }
            Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.Unregister(this);
        }

        public void SetTelegraphVisible(bool visible)
        {
            if (_ring != null) _ring.enabled = visible;
            if (_discRenderer != null) _discRenderer.enabled = visible;
        }

        private Vector3 ResolvePlayerWorldPosition()
        {
            var arena = Object.FindFirstObjectByType<ArenaPosReference>(FindObjectsInactive.Exclude);
            if (arena != null)
            {
                return arena.RelativeArenaPositionToRealPosition(arena.GetPlayerArenaPosition());
            }

            var nara = Object.FindFirstObjectByType<NaraView>(FindObjectsInactive.Exclude);
            return nara != null ? nara.transform.position : Vector3.zero;
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            if (_ring == null) return;
            segments = Mathf.Max(12, segments);
            _ring.positionCount = segments;
            var step = Mathf.PI * 2f / segments;
            for (var i = 0; i < segments; i++)
            {
                var a = i * step;
                var x = center.x + Mathf.Cos(a) * radius;
                var z = center.z + Mathf.Sin(a) * radius;
                _ring.SetPosition(i, new Vector3(x, _yOffset, z));
            }
        }

        private static Mesh BuildFilledDisc(float radius, int segments)
        {
            segments = Mathf.Max(12, segments);
            var mesh = new Mesh { name = "PlayerFootCircleDiscMesh" };
            var verts = new Vector3[segments + 1];
            var tris = new int[segments * 3];
            verts[0] = Vector3.zero;
            var step = Mathf.PI * 2f / segments;
            for (var i = 0; i < segments; i++)
            {
                var a = i * step;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            }
            for (var i = 0; i < segments; i++)
            {
                var i0 = 0;
                var i1 = i + 1;
                var i2 = i == segments - 1 ? 1 : i + 2;
                var t = i * 3;
                tris[t + 0] = i0;
                tris[t + 1] = i2;
                tris[t + 2] = i1;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
