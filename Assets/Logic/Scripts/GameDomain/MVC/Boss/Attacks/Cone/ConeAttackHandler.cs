using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core;
using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Abilitys;

namespace Logic.Scripts.GameDomain.MVC.Boss.Attacks.Cone
{
    public class ConeAttackHandler : IBossAttackHandler, Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility
    {
        private readonly float _radius;
        private readonly float _angleDeg;
        private readonly int _sides;
        private readonly float[] _yaws;
		private class ConeSubView
        {
            public LineRenderer Line;
            public MeshFilter MeshFilter;
            public MeshRenderer MeshRenderer;
            public Mesh Mesh;
        }
		private ConeSubView[] _views;
		private readonly Material _lineMaterial;
		private readonly Material _meshMaterial;
		private Logic.Scripts.GameDomain.MVC.Boss.Telegraph.ITelegraphLayeringService.TelegraphLayer _layer;

		public ConeAttackHandler(float radius, float angleDeg, int sides, float[] yaws, Material lineMaterial, Material meshMaterial)
        {
            _radius = radius;
            _angleDeg = angleDeg;
            _sides = sides;
            _yaws = yaws;
			_lineMaterial = lineMaterial;
			_meshMaterial = meshMaterial;
        }

        public void PrepareTelegraph(Transform parentTransform)
        {
			var layering = Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphLayeringLocator.Service;
			_layer = layering != null ? layering.Register(preferTop: false) : default;

            if (_yaws == null || _yaws.Length == 0) return;
            _views = new ConeSubView[_yaws.Length];
            for (int i = 0; i < _yaws.Length; i++)
            {
                GameObject go = new GameObject("ConeSubActionView");
                go.transform.SetParent(parentTransform, false);

                ConeSubView v = new ConeSubView();
                v.Line = go.AddComponent<LineRenderer>();
				var lineMat = _lineMaterial != null ? new Material(_lineMaterial) : new Material(Shader.Find("Sprites/Default"));
				lineMat.renderQueue += _layer.QueueAdd;
				v.Line.material = lineMat;
                v.Line.useWorldSpace = true;
                v.Line.loop = true;
                v.Line.widthMultiplier = 0.1f;
				// Deixe a cor/controlar via material do ShaderGraph

                v.MeshFilter = go.AddComponent<MeshFilter>();
                v.MeshRenderer = go.AddComponent<MeshRenderer>();
				var meshMat = _meshMaterial != null ? new Material(_meshMaterial) : new Material(Shader.Find("Sprites/Default"));
				meshMat.renderQueue += _layer.QueueAdd;
				v.MeshRenderer.material = meshMat;
                v.Mesh = new Mesh();
                v.Mesh.name = "ConeMesh";
                v.MeshFilter.sharedMesh = v.Mesh;

                Vector3 origin = parentTransform.position;
                Vector3 parentFwd = new Vector3(parentTransform.forward.x, 0f, parentTransform.forward.z);
                if (parentFwd.sqrMagnitude < 1e-6f) parentFwd = Vector3.forward;
                Vector3 forward = Quaternion.Euler(0f, _yaws[i], 0f) * parentFwd;

                Vector3[] outline = ConeArea.GenerateConeOutlinePolygon(origin, forward, _radius, _angleDeg, _sides);
				for (int p = 0; p < outline.Length; p++) outline[p].y = _layer.Y;
                v.Line.positionCount = outline.Length;
                v.Line.SetPositions(outline);

                Vector3[] arc = ConeArea.GenerateConeArcVertices(origin, forward, _radius, _angleDeg, _sides);
				for (int p = 0; p < arc.Length; p++) arc[p].y = _layer.Y;

                Transform mT = v.MeshFilter.transform;
				mT.localPosition = new Vector3(0f, _layer.Y, 0f);
                mT.localRotation = Quaternion.identity;

                // Build triangle fan: vertex 0 = origin, then arc points
                Vector3[] worldVerts = new Vector3[arc.Length + 1];
				worldVerts[0] = new Vector3(origin.x, _layer.Y, origin.z);
                for (int a = 0; a < arc.Length; a++) worldVerts[a + 1] = arc[a];

                Vector3[] localVerts = new Vector3[worldVerts.Length];
                for (int a = 0; a < worldVerts.Length; a++) localVerts[a] = mT.InverseTransformPoint(worldVerts[a]);

                int triCount = (worldVerts.Length - 1) * 3;
                int[] tris = new int[triCount];
                int t = 0;
                for (int a = 1; a < worldVerts.Length - 1; a++)
                {
                    tris[t++] = 0; tris[t++] = a; tris[t++] = a + 1;
                }

                v.Mesh.Clear();
                v.Mesh.vertices = localVerts;
                v.Mesh.triangles = tris;
                v.Mesh.RecalculateNormals();
                v.Mesh.RecalculateBounds();

                _views[i] = v;
            }

            // Start hidden; boss controller will reveal at mid prep
            SetTelegraphVisible(false);
        }

        public bool ComputeHits(ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
        {
            if (_yaws == null || _yaws.Length == 0) return false;
            Vector3 playerWorld = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
            for (int i = 0; i < _yaws.Length; i++)
            {
                Vector3 origin = originTransform.position;
                Vector3 baseFwd = new Vector3(originTransform.forward.x, 0f, originTransform.forward.z);
                if (baseFwd.sqrMagnitude < 1e-6f) baseFwd = Vector3.forward;
                Vector3 forward = Quaternion.Euler(0f, _yaws[i], 0f) * baseFwd;
                if (ConeArea.IsPointInsideCone(origin, forward, _radius, _angleDeg, playerWorld)) return true;
            }
            return false;
        }

        public System.Collections.IEnumerator ExecuteEffects(List<AbilityEffect> effects, ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
        {
            if (effects == null || effects.Count == 0) yield break;
            IEffectable target = arenaReference.NaraController as IEffectable;
            if (target == null) yield break;

            // Apply all effects if any cone hits
            Vector3 playerWorld = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
            bool anyHit = false;
            for (int i = 0; i < _yaws.Length; i++)
            {
                Vector3 origin = originTransform.position;
                Vector3 baseFwd = new Vector3(originTransform.forward.x, 0f, originTransform.forward.z);
                if (baseFwd.sqrMagnitude < 1e-6f) baseFwd = Vector3.forward;
                Vector3 forward = Quaternion.Euler(0f, _yaws[i], 0f) * baseFwd;
                if (ConeArea.IsPointInsideCone(origin, forward, _radius, _angleDeg, playerWorld)) { anyHit = true; break; }
            }
            if (!anyHit) yield break;

            for (int i = 0; i < effects.Count; i++)
            {
                AbilityEffect fx = effects[i];
                fx?.Execute(caster, target);
            }
            yield break;
        }

        public void Cleanup()
        {
            if (_views == null) return;
            for (int i = 0; i < _views.Length; i++)
            {
                if (_views[i] != null)
                {
                    Object.Destroy(_views[i].Line?.gameObject);
                }
            }
            _views = null;

			var layering = Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphLayeringLocator.Service;
			if (layering != null && _layer.Id >= 0) layering.Unregister(_layer.Id);
        }

        public void SetTelegraphVisible(bool visible)
        {
            if (_views == null) return;
            for (int i = 0; i < _views.Length; i++)
            {
                var v = _views[i];
                if (v?.Line != null) v.Line.enabled = visible;
                if (v?.MeshRenderer != null) v.MeshRenderer.enabled = visible;
            }
        }
    }
}


