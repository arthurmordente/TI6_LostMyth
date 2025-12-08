using UnityEngine;
using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core;
using Logic.Scripts.GameDomain.MVC.Abilitys;

namespace Logic.Scripts.GameDomain.MVC.Boss.Attacks.Circle
{
	public sealed class CircleAttackHandler : IBossAttackHandler, Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility
	{
		private readonly float _radius;
		private readonly float _ringWidth;

		private Transform _parent;
		private ArenaPosReference _arena;
		private LineRenderer _ring;
		private MeshFilter _discFilter;
		private MeshRenderer _discRenderer;
		private float _yOffset = 0.05f;
		private int _rqAdd = 0;
		private Material _lineMaterial;
		private Material _meshMaterial;

		public CircleAttackHandler(float radius, float ringWidth, Material lineMaterial, Material meshMaterial)
		{
			_radius = Mathf.Max(0.1f, radius);
			_ringWidth = Mathf.Max(0.02f, ringWidth);
			_lineMaterial = lineMaterial;
			_meshMaterial = meshMaterial;
		}

		public void PrepareTelegraph(Transform parentTransform)
		{
			_parent = parentTransform;
			_arena = Object.FindFirstObjectByType<ArenaPosReference>(FindObjectsInactive.Exclude);

			var go = new GameObject("Circle_Ring");
			go.transform.SetParent(parentTransform, false);
			_ring = go.AddComponent<LineRenderer>();

			var layering = Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphLayeringLocator.Service;
			var layer = layering != null ? layering.Register(preferTop: false) : default;
			_yOffset = layer.Y;
			_rqAdd = layer.QueueAdd;

			var ringMat = _lineMaterial != null ? new Material(_lineMaterial) : new Material(Shader.Find("Sprites/Default"));
			ringMat.renderQueue += _rqAdd;
			_ring.material = ringMat;
			_ring.useWorldSpace = true;
			_ring.loop = true;
			_ring.widthMultiplier = _ringWidth;

			DrawCircle(_parent.position, _radius, 64);

			var discGo = new GameObject("Circle_Fill");
			discGo.transform.SetParent(parentTransform, false);
			_discFilter = discGo.AddComponent<MeshFilter>();
			_discRenderer = discGo.AddComponent<MeshRenderer>();
			var discMat = _meshMaterial != null ? new Material(_meshMaterial) : new Material(Shader.Find("Sprites/Default"));
			discMat.renderQueue += _rqAdd;
			_discRenderer.material = discMat;
			float effectiveRadius = Mathf.Max(0.01f, _radius - _ringWidth * 0.5f);
			_discFilter.sharedMesh = BuildFilledDisc(effectiveRadius, 64);
			_discFilter.transform.position = new Vector3(_parent.position.x, _yOffset, _parent.position.z);

			// Start hidden; boss controller will reveal at mid prep
			SetTelegraphVisible(false);
			Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.Register(this);
		}

		public bool ComputeHits(ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
		{
			if (arenaReference == null || originTransform == null) return false;
			Vector3 playerWorld = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
			Vector3 center = originTransform.position;
			center.y = playerWorld.y;
			float dist = Vector3.Distance(playerWorld, center);
			return dist <= _radius + 1e-4f;
		}

		public System.Collections.IEnumerator ExecuteEffects(List<AbilityEffect> effects, ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
		{
			if (effects == null || effects.Count == 0) yield break;
			if (arenaReference == null) yield break;
			IEffectable target = arenaReference.NaraController as IEffectable;
			if (target == null) yield break;

			Vector3 playerWorld = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
			Vector3 center = originTransform.position;
			center.y = playerWorld.y;
			float dist = Vector3.Distance(playerWorld, center);
			if (dist <= _radius + 1e-4f)
			{
				for (int i = 0; i < effects.Count; i++)
				{
					var fx = effects[i];
					if (fx == null) continue;
					if (fx is IAsyncEffect asyncFx) yield return asyncFx.ExecuteRoutine(caster, target);
					else fx.Execute(caster, target);
				}
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

		private void DrawCircle(Vector3 center, float radius, int segments)
		{
			if (_ring == null) return;
			segments = Mathf.Max(12, segments);
			_ring.positionCount = segments;
			float step = Mathf.PI * 2f / segments;
			float y = _yOffset;
			for (int i = 0; i < segments; i++)
			{
				float a = i * step;
				float x = center.x + Mathf.Cos(a) * radius;
				float z = center.z + Mathf.Sin(a) * radius;
				_ring.SetPosition(i, new Vector3(x, y, z));
			}
		}

		private Mesh BuildFilledDisc(float radius, int segments)
		{
			segments = Mathf.Max(12, segments);
			var mesh = new Mesh { name = "CircleDiscMesh" };
			var verts = new Vector3[segments + 1];
			var tris = new int[segments * 3];
			verts[0] = new Vector3(0f, 0f, 0f);
			float step = Mathf.PI * 2f / segments;
			for (int i = 0; i < segments; i++)
			{
				float a = i * step;
				float x = Mathf.Cos(a) * radius;
				float z = Mathf.Sin(a) * radius;
				verts[i + 1] = new Vector3(x, 0f, z);
			}
			for (int i = 0; i < segments; i++)
			{
				int i0 = 0;
				int i1 = i + 1;
				int i2 = (i == segments - 1) ? 1 : (i + 2);
				int triIdx = i * 3;
				tris[triIdx + 0] = i0;
				tris[triIdx + 1] = i2;
				tris[triIdx + 2] = i1;
			}
			mesh.vertices = verts;
			mesh.triangles = tris;
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}
	}
}


