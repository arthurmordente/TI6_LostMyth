using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	[DefaultExecutionOrder(-10)]
	public sealed class LakiRouletteArenaView : MonoBehaviour, IRouletteArenaVisual
	{
		[Header("Donut Geometry (visual)")]
		[SerializeField] private Vector3 _centerWorld = new Vector3(0f, 7f, 0f);
		[SerializeField] private float _innerRadius = RouletteArenaService.INNER_RADIUS_DEFAULT;
		[SerializeField] private float _outerRadius = RouletteArenaService.OUTER_RADIUS_DEFAULT;
		[SerializeField] private int _sectorCount = 16;
		[SerializeField] private int _radialBands = 2;
		[SerializeField, Range(0f, 1f)] private float _radialSplit01 = 0.6f;

		[Header("Rendering")]
		[SerializeField] private int _angularSmooth = 8;
		[SerializeField] private float _alphaPositive = 0.65f;
		[SerializeField] private float _alphaNegative = 0.65f;
		[SerializeField] private float _alphaNeutral = 0.35f;
		[SerializeField] private float _angularGapDeg = 2f;
		[SerializeField] private float _radialGap = 0.05f;

		private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>(32);
		private readonly List<Color> _baseColors = new List<Color>(32);
		private Material _matTemplate;

		public int TileCount => _sectorCount * _radialBands;

		private void Awake()
		{
			if (_sectorCount * _radialBands != 32)
			{
				_sectorCount = 16;
				_radialBands = 2;
			}
			if (_innerRadius <= 0.01f) _innerRadius = RouletteArenaService.INNER_RADIUS_DEFAULT;
			if (_outerRadius <= _innerRadius + 0.01f) _outerRadius = RouletteArenaService.OUTER_RADIUS_DEFAULT;

			Shader lit = Shader.Find("Universal Render Pipeline/Lit");
			_matTemplate = new Material(lit) { enableInstancing = true };
			BuildTiles();
		}

		public void SetGeometry(Vector3 centerWorld, float innerRadius, float outerRadius, float radialSplit01 = 0.6f)
		{
			_centerWorld = centerWorld;
			_innerRadius = innerRadius;
			_outerRadius = Mathf.Max(_innerRadius + 0.01f, outerRadius);
			_radialSplit01 = Mathf.Clamp01(radialSplit01);
			BuildTiles();
		}

		private void BuildTiles()
		{
			for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
			_renderers.Clear();
			_baseColors.Clear();

			float sectorAngle = 360f / _sectorCount;
			float split = _innerRadius + _radialSplit01 * (_outerRadius - _innerRadius);
			float halfGap = Mathf.Max(0f, _angularGapDeg) * 0.5f;

			int tileIndex = 0;
			for (int s = 0; s < _sectorCount; s++)
			{
				float a0 = s * sectorAngle + halfGap;
				float a1 = (s + 1) * sectorAngle - halfGap;

				for (int band = 0; band < _radialBands; band++)
				{
					float r0 = band == 0 ? _innerRadius : split;
					float r1 = band == 0 ? split : _outerRadius;
					float rMin = Mathf.Min(r0, r1) + Mathf.Max(0f, _radialGap);
					float rMax = Mathf.Max(r0, r1) - Mathf.Max(0f, _radialGap);
					if (rMax <= rMin) rMax = rMin + 0.005f;

					GameObject go = new GameObject($"Tile_{tileIndex:D2}_S{s}_B{band}");
					go.transform.SetParent(transform, false);
					go.transform.position = _centerWorld;
					var mf = go.AddComponent<MeshFilter>();
					var mr = go.AddComponent<MeshRenderer>();
					mr.sharedMaterial = new Material(_matTemplate);
					mf.sharedMesh = GenerateRingSectorMesh(rMin, rMax, a0, a1, _angularSmooth);

					_renderers.Add(mr);
					_baseColors.Add(Color.clear);
					tileIndex++;
				}
			}
		}

		private static Mesh GenerateRingSectorMesh(float innerR, float outerR, float degStart, float degEnd, int arcSegments)
		{
			arcSegments = Mathf.Max(1, arcSegments);
			int vertsPerRing = arcSegments + 1;
			int vertexCount = vertsPerRing * 2;
			int triCount = arcSegments * 2;

			var verts = new Vector3[vertexCount];
			var tris = new int[triCount * 3];
			var uvs = new Vector2[vertexCount];

			float a0 = degStart * Mathf.Deg2Rad;
			float a1 = degEnd * Mathf.Deg2Rad;
			float da = (a1 - a0) / arcSegments;

			int vi = 0;
			for (int i = 0; i < vertsPerRing; i++)
			{
				float a = a0 + da * i;
				float ca = Mathf.Cos(a);
				float sa = Mathf.Sin(a);
				verts[vi + 0] = new Vector3(ca * innerR, 0f, sa * innerR);
				verts[vi + 1] = new Vector3(ca * outerR, 0f, sa * outerR);
				uvs[vi + 0] = new Vector2((float)i / arcSegments, 0f);
				uvs[vi + 1] = new Vector2((float)i / arcSegments, 1f);
				vi += 2;
			}

			int ti = 0;
			for (int i = 0; i < arcSegments; i++)
			{
				int i0 = i * 2;
				int i1 = i0 + 1;
				int i2 = i0 + 2;
				int i3 = i0 + 3;
				// Winding adjusted to ensure normals point upwards
				tris[ti++] = i0; tris[ti++] = i3; tris[ti++] = i1;
				tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i3;
			}

			var mesh = new Mesh
			{
				name = "RingSector",
				indexFormat = vertexCount > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16
			};
			mesh.SetVertices(verts);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(tris, 0);
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			return mesh;
		}

		public void RefreshFrom(RouletteArenaService service)
		{
			if (service == null) return;
			int tiles = service.TileCount;
			for (int i = 0; i < _renderers.Count && i < tiles; i++)
			{
				var type = service.GetTileEffect(i);
				Color c;
				switch (type)
				{
					case RouletteArenaService.TileEffectType.Positive:
						c = new Color(0.2f, 1f, 0.2f, _alphaPositive);
						break;
					case RouletteArenaService.TileEffectType.Negative:
						c = new Color(1f, 0.2f, 0.2f, _alphaNegative);
						break;
					default:
						c = new Color(0.82f, 0.82f, 0.82f, _alphaNeutral);
						break;
				}
				var mat = _renderers[i].sharedMaterial;
				if (mat != null)
				{
					if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
					else if (mat.HasProperty("_Color")) mat.color = c;
					_baseColors[i] = c;
				}
			}
		}

		public void SetEmphasis(System.Collections.Generic.ICollection<int> tileIndices, float t01, float extraIntensity = 0.75f)
		{
			if (tileIndices == null || _renderers.Count == 0) return;
			float k = Mathf.Clamp01(t01);
			for (int i = 0; i < _renderers.Count; i++)
			{
				Color baseC = (i < _baseColors.Count) ? _baseColors[i] : Color.white;
				bool isEmphasized = tileIndices.Contains(i);
				Color c;
				if (isEmphasized) {
					float lighten = Mathf.Lerp(1f, 1.35f, k);
					float a = baseC.a;
					c = new Color(
						Mathf.Clamp01(baseC.r * lighten),
						Mathf.Clamp01(baseC.g * lighten),
						Mathf.Clamp01(baseC.b * lighten),
						a
					);
				} else {
					float darken = Mathf.Lerp(1f, 0.65f, k);
					float a = Mathf.Clamp01(Mathf.Lerp(baseC.a, baseC.a * 0.9f, k));
					c = new Color(
						Mathf.Clamp01(baseC.r * darken),
						Mathf.Clamp01(baseC.g * darken),
						Mathf.Clamp01(baseC.b * darken),
						a
					);
				}
				var mat = _renderers[i].sharedMaterial;
				if (mat != null)
				{
					if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
					else if (mat.HasProperty("_Color")) mat.color = c;
				}
			}
		}

		public Vector3 GetTileWorldCenter(int tileIndex)
		{
			if (tileIndex < 0) tileIndex = 0;
			int max = _sectorCount * _radialBands;
			if (max <= 0) return _centerWorld;
			tileIndex = tileIndex % max;

			float sectorAngle = 360f / _sectorCount;
			float split = _innerRadius + _radialSplit01 * (_outerRadius - _innerRadius);
			float halfGap = Mathf.Max(0f, _angularGapDeg) * 0.5f;

			int sector = tileIndex / _radialBands;
			int band = tileIndex % _radialBands;
			float a0 = sector * sectorAngle + halfGap;
			float a1 = (sector + 1) * sectorAngle - halfGap;
			float amidDeg = 0.5f * (a0 + a1);
			float amid = amidDeg * Mathf.Deg2Rad;

			float r0 = band == 0 ? _innerRadius : split;
			float r1 = band == 0 ? split : _outerRadius;
			float rMin = Mathf.Min(r0, r1) + Mathf.Max(0f, _radialGap);
			float rMax = Mathf.Max(r0, r1) - Mathf.Max(0f, _radialGap);
			if (rMax <= rMin) rMax = rMin + 0.005f;
			float rMid = 0.5f * (rMin + rMax);

			float cx = _centerWorld.x + Mathf.Cos(amid) * rMid;
			float cz = _centerWorld.z + Mathf.Sin(amid) * rMid;
			return new Vector3(cx, _centerWorld.y, cz);
		}
	}
}


