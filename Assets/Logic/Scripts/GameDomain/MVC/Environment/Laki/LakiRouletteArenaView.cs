using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	[DefaultExecutionOrder(-10)]
	public sealed class LakiRouletteArenaView : MonoBehaviour, IRouletteArenaVisual
	{
		[Header("Donut Geometry (visual)")]
		[SerializeField] private Vector3 _centerWorld = new Vector3(0f, 7f, 0f);
		[SerializeField] private float _innerRadius = RouletteArenaService.INNER_RADIUS_DEFAULT;
		[SerializeField] private float _outerRadius = RouletteArenaService.OUTER_RADIUS_DEFAULT;
		[SerializeField] private int _sectorCount = 8;
		[SerializeField] private int _radialBands = 2;
		[SerializeField, Range(0f, 1f)] private float _radialSplit01 = 0.6f;
		[SerializeField] private float _arcStartDeg = 180f;
		[SerializeField] private float _arcDeg = 180f;

		[Header("Rendering")]
		[SerializeField] private int _angularSmooth = 8;
		[SerializeField] private float _alphaPositive = 0.65f;
		[SerializeField] private float _alphaNegative = 0.65f;
		[SerializeField] private float _alphaNeutral = 0.35f;
		[SerializeField] private float _angularGapDeg = 2f;
		[SerializeField] private float _radialGap = 0.05f;

		[Header("Tile Info Canvas")]
		[SerializeField] private float _canvasScale       = 0.004f;
		[SerializeField] private float _canvasHeightOffset = 0.12f;
		[SerializeField] private float _slotSpacing       = 80f;
		// Populated at runtime via SetTileEffectVisuals – not serialized because
		// LakiRouletteArenaView is created programmatically (no prefab).
		private TileEffectSlotDef[] _effectSlotDefs = new TileEffectSlotDef[0];

		/// <summary>Inspector-configurable data for one tile-effect visual slot.</summary>
		[System.Serializable]
		public struct TileEffectSlotDef
		{
			public RouletteArenaService.TileEffectType EffectType;
			[Tooltip("Icon shown to the left of the label (null = no image)")]
			public Sprite Icon;
			[Tooltip("Label text shown beside the icon")]
			public string Label;
		}

		private struct TileInfoCanvas { public Transform SlotsContainer; }
		private TileInfoCanvas[] _tileCanvases;

		private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>(16);
		private readonly List<Color> _baseColors = new List<Color>(16);
		private Material _matTemplate;

		public int TileCount => _sectorCount * _radialBands;

		/// <summary>
		/// Call this from LakiArenaBossBootstrap after creating the view and before RefreshFrom.
		/// Builds the slot definition table from the configured effect pools so each tile canvas
		/// knows which icon+label to show for each possible effect.
		/// </summary>
		public void SetTileEffectVisuals(
			System.Collections.Generic.IList<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> positiveEffects,
			System.Collections.Generic.IList<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> negativeEffects)
		{
			var defs = new System.Collections.Generic.List<TileEffectSlotDef>();

			if (positiveEffects != null)
				foreach (var e in positiveEffects)
					if (e != null)
						defs.Add(new TileEffectSlotDef
						{
							EffectType = RouletteArenaService.TileEffectType.Positive,
							Icon       = e.TileIcon,
							Label      = string.IsNullOrEmpty(e.Name) ? "Efeito Positivo" : e.Name,
						});

			if (negativeEffects != null)
				foreach (var e in negativeEffects)
					if (e != null)
						defs.Add(new TileEffectSlotDef
						{
							EffectType = RouletteArenaService.TileEffectType.Negative,
							Icon       = e.TileIcon,
							Label      = string.IsNullOrEmpty(e.Name) ? "Efeito Negativo" : e.Name,
						});

			_effectSlotDefs = defs.ToArray();
		}

		private void Awake()
		{
			if (_sectorCount <= 0) _sectorCount = 8;
			if (_radialBands <= 0) _radialBands = 2;
			if (_innerRadius <= 0.01f) _innerRadius = RouletteArenaService.INNER_RADIUS_DEFAULT;
			if (_outerRadius <= _innerRadius + 0.01f) _outerRadius = RouletteArenaService.OUTER_RADIUS_DEFAULT;
			_arcDeg = Mathf.Clamp(_arcDeg, 1f, 360f);

			Shader lit = Shader.Find("Universal Render Pipeline/Lit");
			_matTemplate = new Material(lit) { enableInstancing = true };
			BuildTiles();
		}

		public void SetGeometry(Vector3 centerWorld, float innerRadius, float outerRadius, float radialSplit01 = 0.6f, float arcStartDeg = 0f, float arcDeg = 180f)
		{
			_centerWorld = centerWorld;
			_innerRadius = innerRadius;
			_outerRadius = Mathf.Max(_innerRadius + 0.01f, outerRadius);
			_radialSplit01 = Mathf.Clamp01(radialSplit01);
			_arcStartDeg = arcStartDeg;
			_arcDeg = Mathf.Clamp(arcDeg, 1f, 360f);
			BuildTiles();
		}

		private void BuildTiles()
		{
			for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
			_renderers.Clear();
			_baseColors.Clear();

			int total = _sectorCount * _radialBands;
			_tileCanvases = new TileInfoCanvas[total];

			float sectorAngle = _arcDeg / _sectorCount;
			float split = _innerRadius + _radialSplit01 * (_outerRadius - _innerRadius);
			float halfGap = Mathf.Max(0f, _angularGapDeg) * 0.5f;

			int tileIndex = 0;
			for (int s = 0; s < _sectorCount; s++)
			{
				float a0 = _arcStartDeg + s * sectorAngle + halfGap;
				float a1 = _arcStartDeg + (s + 1) * sectorAngle - halfGap;

				for (int band = 0; band < _radialBands; band++)
				{
					float r0 = band == 0 ? _innerRadius : split;
					float r1 = band == 0 ? split : _outerRadius;
					float rMin = Mathf.Min(r0, r1) + Mathf.Max(0f, _radialGap);
					float rMax = Mathf.Max(r0, r1) - Mathf.Max(0f, _radialGap);
					if (rMax <= rMin) rMax = rMin + 0.005f;

					// Place each tile's pivot at its own geometric centre so that
					// scaling localScale.x produces a flip around the tile itself.
					float midAngle = (a0 + a1) * 0.5f * Mathf.Deg2Rad;
					float midR     = (rMin + rMax) * 0.5f;
					Vector3 tileCenter = _centerWorld + new Vector3(
						Mathf.Cos(midAngle) * midR, 0f, Mathf.Sin(midAngle) * midR);
					Vector3 pivotOffset = tileCenter - _centerWorld;

					GameObject go = new GameObject($"Tile_{tileIndex:D2}_S{s}_B{band}");
					go.transform.SetParent(transform, false);
					go.transform.position = tileCenter;
					var mf = go.AddComponent<MeshFilter>();
					var mr = go.AddComponent<MeshRenderer>();
					mr.sharedMaterial = new Material(_matTemplate);
					mf.sharedMesh = GenerateRingSectorMesh(rMin, rMax, a0, a1, _angularSmooth, pivotOffset);

					_renderers.Add(mr);
					_baseColors.Add(Color.clear);
					_tileCanvases[tileIndex] = BuildTileCanvas(go.transform, tileCenter, band);
					tileIndex++;
				}
			}
		}

		/// <summary>
		/// Creates the world-space canvas for one tile. Slots are NOT created here –
		/// they are rebuilt dynamically by <see cref="RefreshTileCanvas"/> whenever effects change.
		/// band 0 = inner ring, band 1 = outer ring (affects VLG spacing).
		/// </summary>
		private TileInfoCanvas BuildTileCanvas(Transform tileTr, Vector3 tileCenter, int band)
		{
			var canvasGO = new GameObject("TileInfoCanvas");
			canvasGO.transform.SetParent(tileTr, false);
			canvasGO.transform.localPosition = new Vector3(0f, _canvasHeightOffset, 0f);

			// Lie flat on the arena plane, facing radially outward (same as suit labels)
			Vector3 outward = tileCenter - _centerWorld;
			outward.y = 0f;
			float yAngle = outward.sqrMagnitude > 0.001f
				? Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg + 180f
				: 0f;
			canvasGO.transform.localRotation = Quaternion.Euler(90f, yAngle, 0f);

			var canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			var canvasRt = canvasGO.GetComponent<RectTransform>();
			canvasRt.sizeDelta = new Vector2(1400f, 900f);
			float s = _canvasScale > 0f ? _canvasScale : 0.004f;
			canvasGO.transform.localScale = new Vector3(s, s, s);

			// Container anchored to canvas centre, shifted right (+X) to sit over the tile
			var containerGO = new GameObject("SlotsContainer");
			containerGO.transform.SetParent(canvasGO.transform, false);
			var containerRt = containerGO.AddComponent<RectTransform>();
			containerRt.anchorMin = new Vector2(0.5f, 0.5f);
			containerRt.anchorMax = new Vector2(0.5f, 0.5f);
			containerRt.pivot     = new Vector2(0.5f, 0.5f);
			containerRt.anchoredPosition = new Vector2(250f, 0f);
			containerRt.sizeDelta = new Vector2(1380f, 0f); // height driven by ContentSizeFitter

			var vlg = containerGO.AddComponent<VerticalLayoutGroup>();
			vlg.childAlignment        = TextAnchor.MiddleCenter;
			vlg.childControlWidth     = false;
			vlg.childControlHeight    = false;
			vlg.childForceExpandWidth = false;
			vlg.childForceExpandHeight= false;
			// Inner tiles (band 0) are narrower radially → more vertical spacing needed
			vlg.spacing = band == 0 ? 300f : 200f;

			var csf = containerGO.AddComponent<ContentSizeFitter>();
			csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
			csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

			return new TileInfoCanvas { SlotsContainer = containerGO.transform };
		}

		/// <summary>
		/// Appends a single image+text row into the slots container.
		/// Icon is hidden (transparent) when <paramref name="icon"/> is null.
		/// </summary>
		private static void AppendSlotRow(Transform container, string label, Sprite icon)
		{
			const float rowW   = 1380f;
			const float rowH   = 320f;
			const float iconSz = 320f;
			const float gap    = 180f;
			const float pad    = 12f;

			var rowGO = new GameObject("Slot");
			rowGO.transform.SetParent(container, false);
			var rowRt = rowGO.AddComponent<RectTransform>();
			rowRt.sizeDelta = new Vector2(rowW, rowH);

			var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
			hlg.childAlignment        = TextAnchor.MiddleLeft;
			hlg.childControlWidth     = false;
			hlg.childControlHeight    = false;
			hlg.childForceExpandWidth = false;
			hlg.childForceExpandHeight= false;
			hlg.spacing               = gap;
			hlg.padding               = new RectOffset((int)pad, (int)pad, 0, 0);

			// Icon (hidden when no sprite)
			var imgGO = new GameObject("Icon");
			imgGO.transform.SetParent(rowGO.transform, false);
			var imgRt = imgGO.AddComponent<RectTransform>();
			imgRt.sizeDelta = new Vector2(iconSz, iconSz);
			var img = imgGO.AddComponent<Image>();
			img.sprite        = icon;
			img.preserveAspect= true;
			img.color         = icon != null ? Color.white : Color.clear;

			// Label — single line, fills remaining row width
			float textWidth = rowW - iconSz - gap - pad * 2f;
			var txtGO = new GameObject("Label");
			txtGO.transform.SetParent(rowGO.transform, false);
			var txtRt = txtGO.AddComponent<RectTransform>();
			txtRt.sizeDelta = new Vector2(textWidth, rowH);
			var tmp = txtGO.AddComponent<TextMeshProUGUI>();
			tmp.text               = label ?? "";
			tmp.fontSize           = 120f;
			tmp.color              = Color.black;
			tmp.alignment          = TextAlignmentOptions.MidlineLeft;
			tmp.enableAutoSizing   = false;
			tmp.enableWordWrapping = false;
			tmp.overflowMode       = TextOverflowModes.Ellipsis;
		}

		/// <param name="pivotOffset">Subtracted from every vertex so that (0,0,0) in local space is the tile's geometric centre.</param>
		private static Mesh GenerateRingSectorMesh(float innerR, float outerR, float degStart, float degEnd, int arcSegments, Vector3 pivotOffset = default)
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
				verts[vi + 0] = new Vector3(ca * innerR, 0f, sa * innerR) - pivotOffset;
				verts[vi + 1] = new Vector3(ca * outerR, 0f, sa * outerR) - pivotOffset;
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
			CacheTileEffects(service);
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

				// Update tile info canvas text and images
				if (_tileCanvases != null && i < _tileCanvases.Length)
					RefreshTileCanvas(i, type);
			}
		}

		/// <summary>
		/// Clears the tile's slot container and rebuilds one row per matching
		/// <see cref="TileEffectSlotDef"/> entry. The VLG+ContentSizeFitter centres
		/// them vertically regardless of how many there are.
		/// </summary>
		private void RefreshTileCanvas(int i, RouletteArenaService.TileEffectType type)
		{
			var container = _tileCanvases[i].SlotsContainer;
			if (container == null) return;

			// Remove previous slots
			for (int c = container.childCount - 1; c >= 0; c--)
				Destroy(container.GetChild(c).gameObject);

			if (_effectSlotDefs == null) return;

			foreach (var def in _effectSlotDefs)
			{
				if (def.EffectType != type) continue;
				AppendSlotRow(container, def.Label, def.Icon);
			}

			// Force the layout to recalculate immediately so it is correct this frame
			UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
				container.GetComponent<RectTransform>());
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

		// ─── Tile index from world position ──────────────────────────────────────

		/// <summary>Computes the tile index for a world position using the same polar-coordinate math as RouletteArenaService.</summary>
		public int ComputeTileIndex(Vector3 worldPos)
		{
			Vector2 rel = new Vector2(worldPos.x - _centerWorld.x, worldPos.z - _centerWorld.z);
			float r = rel.magnitude;
			if (r < _innerRadius || r > _outerRadius) return -1;

			float split = _innerRadius + _radialSplit01 * (_outerRadius - _innerRadius);
			float theta = Mathf.Atan2(rel.y, rel.x);
			if (theta < 0f) theta += 2f * Mathf.PI;

			float arcStartRad = _arcStartDeg * Mathf.Deg2Rad;
			float arcRad = Mathf.Clamp(_arcDeg, 1f, 360f) * Mathf.Deg2Rad;
			float sectorAngleRad = arcRad / Mathf.Max(1, _sectorCount);

			float relTheta = theta - arcStartRad;
			if (relTheta < 0f) relTheta += 2f * Mathf.PI;
			if (relTheta >= arcRad) return -1;

			int sectorIndex = Mathf.Clamp(Mathf.FloorToInt(relTheta / sectorAngleRad), 0, _sectorCount - 1);
			int band = r < split ? 0 : 1;
			return sectorIndex * _radialBands + band;
		}

		// ─── Tile effect cache ────────────────────────────────────────────────────

		private RouletteArenaService.TileEffectType[] _cachedTileEffects;

		public void CacheTileEffects(RouletteArenaService service)
		{
			if (service == null) return;
			int count = service.TileCount;
			if (_cachedTileEffects == null || _cachedTileEffects.Length != count)
				_cachedTileEffects = new RouletteArenaService.TileEffectType[count];
			for (int i = 0; i < count; i++)
				_cachedTileEffects[i] = service.GetTileEffect(i);
		}

		public RouletteArenaService.TileEffectType GetCachedTileEffect(int tileIndex)
		{
			if (_cachedTileEffects == null || tileIndex < 0 || tileIndex >= _cachedTileEffects.Length)
				return RouletteArenaService.TileEffectType.Neutral;
			return _cachedTileEffects[tileIndex];
		}

		// ─── Suit overlay ─────────────────────────────────────────────────────────

		private TextMeshPro[] _suitLabels;

		public void InitSuitOverlay()
		{
			DestroySuitOverlay();
			int count = _renderers.Count;
			_suitLabels = new TextMeshPro[count];
			for (int i = 0; i < count; i++)
			{
				if (_renderers[i] == null) continue;
				Vector3 tileCenter = GetTileWorldCenter(i);
				var go = new GameObject($"SuitLabel_{i}");
				go.transform.SetParent(_renderers[i].transform, false);
				// Tile pivot is now at tileCenter, so (0, 0.5f, 0) places the label
				// at the tile centre slightly above the surface.
				go.transform.localPosition = new Vector3(0f, 0.5f, 0f);
				// Radial direction comes from the tile centre relative to the arena centre.
				Vector3 outward = tileCenter - _centerWorld;
				outward.y = 0f;
				float yAngle = outward.sqrMagnitude > 0.001f
					? Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg + 180f
					: 0f;
				go.transform.localRotation = Quaternion.Euler(90f, yAngle, 0f);
				var tmp = go.AddComponent<TextMeshPro>();
				tmp.alignment = TextAlignmentOptions.Center;
				tmp.fontSize = 30f;
				tmp.color = Color.black;
				tmp.enableAutoSizing = false;
				go.SetActive(false);
				_suitLabels[i] = tmp;
			}
		}

		public void DestroySuitOverlay()
		{
			if (_suitLabels == null) return;
			for (int i = 0; i < _suitLabels.Length; i++)
			{
				if (_suitLabels[i] != null) Destroy(_suitLabels[i].gameObject);
			}
			_suitLabels = null;
		}

		/// <summary>Animates all tiles: flash white → show suit number → flash back → hide number.</summary>
		public Task AnimateSuitRevealAsync(int[] suits, int flipMs, int holdMs)
		{
			return AnimateSuitRevealTilesAsync(null, suits, flipMs, holdMs);
		}

		/// <summary>Same as AnimateSuitRevealAsync but restricted to the given tile indices (null = all).</summary>
		public Task AnimateSuitRevealTilesAsync(ICollection<int> indices, int[] suits, int flipMs, int holdMs)
		{
			int halfMs = Mathf.Max(50, flipMs / 2);
			var tasks  = new System.Collections.Generic.List<Task>();
			for (int i = 0; i < _renderers.Count; i++)
			{
				if (indices != null && !indices.Contains(i)) continue;
				tasks.Add(FlipSingleTileAsync(i, suits, halfMs, holdMs));
			}
			return Task.WhenAll(tasks);
		}

		/// <summary>Flips a single tile independently: fold → show number → unfold → hold → fold → hide number → unfold.</summary>
		private async Task FlipSingleTileAsync(int i, int[] suits, int halfMs, int holdMs)
		{
			if (i < 0 || i >= _renderers.Count || _renderers[i] == null) return;
			var tr    = _renderers[i].transform;
			int steps = Mathf.Max(3, halfMs / 16);
			int stepMs = Mathf.Max(16, halfMs / steps);

			// Fold: scale X 1 → 0
			for (int s = steps; s >= 0; s--)
			{
				var sc = tr.localScale; sc.x = (float)s / steps; tr.localScale = sc;
				await Task.Delay(stepMs);
			}

			// Midpoint: show number
			if (suits != null && _suitLabels != null && i < _suitLabels.Length && i < suits.Length && _suitLabels[i] != null)
			{
				_suitLabels[i].SetText(suits[i].ToString());
				_suitLabels[i].gameObject.SetActive(true);
			}

			// Unfold: scale X 0 → 1
			for (int s = 0; s <= steps; s++)
			{
				var sc = tr.localScale; sc.x = (float)s / steps; tr.localScale = sc;
				await Task.Delay(stepMs);
			}

			await Task.Delay(Mathf.Max(100, holdMs));

			// Fold: scale X 1 → 0
			for (int s = steps; s >= 0; s--)
			{
				var sc = tr.localScale; sc.x = (float)s / steps; tr.localScale = sc;
				await Task.Delay(stepMs);
			}

			// Midpoint: hide number
			if (_suitLabels != null && i < _suitLabels.Length && _suitLabels[i] != null)
				_suitLabels[i].gameObject.SetActive(false);

			// Unfold: scale X 0 → 1 (tile returns to normal)
			for (int s = 0; s <= steps; s++)
			{
				var sc = tr.localScale; sc.x = (float)s / steps; tr.localScale = sc;
				await Task.Delay(stepMs);
			}

			// Restore exact scale
			var final = tr.localScale; final.x = 1f; tr.localScale = final;
		}

		public Vector3 GetTileWorldCenter(int tileIndex)
		{
			if (tileIndex < 0) tileIndex = 0;
			int max = _sectorCount * _radialBands;
			if (max <= 0) return _centerWorld;
			tileIndex = tileIndex % max;

			float sectorAngle = _arcDeg / _sectorCount;
			float split = _innerRadius + _radialSplit01 * (_outerRadius - _innerRadius);
			float halfGap = Mathf.Max(0f, _angularGapDeg) * 0.5f;

			int sector = tileIndex / _radialBands;
			int band = tileIndex % _radialBands;
			float a0 = _arcStartDeg + sector * sectorAngle + halfGap;
			float a1 = _arcStartDeg + (sector + 1) * sectorAngle - halfGap;
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


