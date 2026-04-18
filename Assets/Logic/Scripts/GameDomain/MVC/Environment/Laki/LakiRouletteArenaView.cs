using System.Collections.Generic;
using System.Threading.Tasks;
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	[DefaultExecutionOrder(-10)]
	public sealed class LakiRouletteArenaView : MonoBehaviour, IRouletteArenaVisual
	{
		[Header("Donut Geometry (visual)")]
		[SerializeField] private Vector3 _centerWorld = new Vector3(0f, 0.5f, -4f);
		[SerializeField] private float _innerRadius = RouletteArenaService.INNER_RADIUS_DEFAULT;
		[SerializeField] private float _outerRadius = RouletteArenaService.OUTER_RADIUS_DEFAULT;
		[SerializeField] private int _sectorCount = 8;
		[SerializeField] private int _radialBands = 2;
		[SerializeField, Range(0f, 1f), Tooltip("Unused. Split uses TILE_RADIAL_DEPTH + 2.5% outer gap (see RouletteArenaService).")]
		private float _radialSplit01 = 0.6f;
		[SerializeField] private float _arcStartDeg = 180f;
		[SerializeField] private float _arcDeg = 180f;

		[Header("Tile prefabs (optional)")]
		[SerializeField] private CombatAttackVisualCatalogSO _attackVisualCatalog;

		[Header("Rendering")]
		[SerializeField] private int _angularSmooth = 8;
		[SerializeField] private float _alphaPositive = 0.65f;
		[SerializeField] private float _alphaNegative = 0.65f;
		[SerializeField] private float _alphaNeutral = 0.35f;
		[SerializeField] private float _radialGap = 0.05f;

		[Header("Tile Info Canvas")]
		[SerializeField] private float _canvasScale       = 0.004f;
		[SerializeField] private float _canvasHeightOffset = 0.12f;
		[SerializeField] private float _slotSpacing = 80f;

		/// <summary>Base layout factor (post −33%); multiplied by <see cref="TileCanvasSizeBoost"/> for effective UI size.</summary>
		private const float TileCanvasLayoutScale = 0.67f;
		/// <summary>+50% vs current scaled layout (1.5× on top of <see cref="TileCanvasLayoutScale"/>).</summary>
		private const float TileCanvasSizeBoost = 1.5f;
		private const float TileCanvasEulerX = 90f;
		private const float TileCanvasEulerY = 0f;
		private const float TileCanvasEulerZ = -80f;
		private const float OuterTileSurfaceLocalX = 0.5f;

		private struct TileInfoCanvas { public Transform SlotsContainer; }
		private TileInfoCanvas[] _tileCanvases;

		private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>(16);
		private readonly List<Color> _baseColors = new List<Color>(16);
		private readonly List<bool> _surfaceFromCatalog = new List<bool>(16);
		private Material _matTemplate;
		private CombatAttackVisualCatalogSO _resolvedCatalog;
		private Transform[] _tileRoots;
		private RouletteArenaService.TileEffectType[] _lastVisualTileTypes;

		public int TileCount => _sectorCount * _radialBands;

		/// <summary>Radial band count (inner/outer ring). Matches tile index layout: sector = index / bands, band = index % bands.</summary>
		public int RadialBands => Mathf.Max(1, _radialBands);

		/// <summary>Call before <see cref="SetGeometry"/> when the view is created at runtime (e.g. <see cref="LakiArenaBossBootstrap"/>). Inspector-assigned <see cref="_attackVisualCatalog"/> is used automatically when present.</summary>
		public void SetAttackVisualCatalog(CombatAttackVisualCatalogSO catalog)
		{
			_attackVisualCatalog = catalog;
			_resolvedCatalog = ResolveCatalog();
		}

		private CombatAttackVisualCatalogSO ResolveCatalog()
		{
			if (_attackVisualCatalog != null) return _attackVisualCatalog;
			var fromResources = Resources.Load<CombatAttackVisualCatalogSO>("CombatAttackVisualCatalog");
			if (fromResources != null) return fromResources;
#if UNITY_EDITOR
			return AssetDatabase.LoadAssetAtPath<CombatAttackVisualCatalogSO>(
				"Assets/Logic/Scripts/GameDomain/MVC/Boss/Visuals/CombatAttackVisualCatalog.asset");
#else
			return null;
#endif
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
			_resolvedCatalog = ResolveCatalog();
			// Do not BuildTiles here: bootstrap calls SetAttackVisualCatalog then SetGeometry.
			// An Awake build runs before catalog is injected and uses procedural meshes; deferred Destroy
			// would leave both procedural and catalog visible for a frame (or longer).
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

		private const float TileRootWorldY = -0.05f;

		/// <summary>World position of the tile root: layout centre with fixed Y (outer mesh shift is on TileSurface local X).</summary>
		private Vector3 TileRootWorldFromLayout(Vector3 layoutCenter)
		{
			Vector3 p = layoutCenter;
			p.y = TileRootWorldY;
			return p;
		}

		private float ReferenceSectorMidDegrees()
		{
			float sectorAngle = _arcDeg / Mathf.Max(1, _sectorCount);
			float halfGap = RouletteArenaService.ComputeTileAngularHalfGapDeg(_arcDeg, _sectorCount);
			float a0 = _arcStartDeg + halfGap;
			float a1 = _arcStartDeg + sectorAngle - halfGap;
			return 0.5f * (a0 + a1);
		}

		private void ComputeTileLayoutForIndex(int tileIndex, out int band, out float a0, out float a1, out float rMin, out float rMax, out Vector3 pivotOffset, out float midDeg, out Vector3 tileCenter)
		{
			int sector = tileIndex / Mathf.Max(1, _radialBands);
			band = tileIndex % Mathf.Max(1, _radialBands);
			float sectorAngle = _arcDeg / Mathf.Max(1, _sectorCount);
			float halfGap = RouletteArenaService.ComputeTileAngularHalfGapDeg(_arcDeg, _sectorCount);
			a0 = _arcStartDeg + sector * sectorAngle + halfGap;
			a1 = _arcStartDeg + (sector + 1) * sectorAngle - halfGap;
			RouletteArenaService.ComputeBandRadialExtents(
				_innerRadius, _outerRadius, band, _radialGap, out rMin, out rMax);
			float midAngle = (a0 + a1) * 0.5f * Mathf.Deg2Rad;
			float midR = (rMin + rMax) * 0.5f;
			tileCenter = _centerWorld + new Vector3(Mathf.Cos(midAngle) * midR, 0f, Mathf.Sin(midAngle) * midR);
			pivotOffset = tileCenter - _centerWorld;
			midDeg = 0.5f * (a0 + a1);
		}

		/// <summary>Outer radial band (last band index when there are at least two rings).</summary>
		private bool IsOuterTileBand(int band) => _radialBands >= 2 && band == _radialBands - 1;

		private Vector3 TileSurfaceLocalPosition(int band) =>
			IsOuterTileBand(band) ? new Vector3(OuterTileSurfaceLocalX, 0f, 0f) : Vector3.zero;

		/// <summary>Creates TileSurface under <paramref name="tileRoot"/> (anchored at tile centre). Catalog prefabs are canonical sector-0 wedges; tileRoot Y rotation aligns them per sector.</summary>
		private MeshRenderer BuildTileSurface(Transform tileRoot, int band, RouletteArenaService.TileEffectType effectType, float a0, float a1, float rMin, float rMax, Vector3 pivotOffset, float midDeg, out bool usesCatalogPrefab)
		{
			usesCatalogPrefab = false;
			Transform old = tileRoot.Find("TileSurface");
			if (old != null) DestroyImmediate(old.gameObject);

			var surfaceGo = new GameObject("TileSurface");
			surfaceGo.transform.SetParent(tileRoot, false);
			Vector3 surfaceLocalPos = TileSurfaceLocalPosition(band);
			surfaceGo.transform.localPosition = surfaceLocalPos;
			surfaceGo.transform.localRotation = Quaternion.identity;
			surfaceGo.transform.localScale = Vector3.one;
			var surfaceTr = surfaceGo.transform;

			bool inner = band == 0;
			bool catalogTilesAvailable = HasAnyLakiRouletteCatalogPrefab();
			GameObject tilePrefab = ResolveLakiRouletteTilePrefab(inner, effectType);

			if (tilePrefab != null)
			{
				tileRoot.localRotation = Quaternion.Euler(0f, ReferenceSectorMidDegrees() - midDeg, 0f);
				var inst = Instantiate(tilePrefab, surfaceTr, false);
				inst.name = "MeshPrefab";
				inst.transform.localPosition = Vector3.zero;
				inst.transform.localRotation = Quaternion.identity;
				inst.transform.localScale = Vector3.one;
				var mrs = inst.GetComponentsInChildren<MeshRenderer>(true);
				MeshRenderer mr = null;
				for (int i = 0; i < mrs.Length; i++)
				{
					var mfInst = mrs[i].GetComponent<MeshFilter>();
					if (mfInst != null && mfInst.sharedMesh != null)
					{
						mr = mrs[i];
						break;
					}
				}
				if (mr == null && mrs.Length > 0) mr = mrs[0];
				if (mr != null)
				{
					for (int i = 0; i < mrs.Length; i++)
						mrs[i].enabled = (mrs[i] == mr);
					if (mr.sharedMaterial != null)
						mr.sharedMaterial = new Material(mr.sharedMaterial);
					// Instantiate must not override TileSurface transform; re-apply after prefab setup.
					surfaceTr.localPosition = surfaceLocalPos;
					surfaceTr.localRotation = Quaternion.identity;
					surfaceTr.localScale = Vector3.one;
					usesCatalogPrefab = true;
					return mr;
				}
				Destroy(inst);
			}

			tileRoot.localRotation = Quaternion.identity;
			if (catalogTilesAvailable)
				return null;

			var mf = surfaceGo.AddComponent<MeshFilter>();
			var mrProc = surfaceGo.AddComponent<MeshRenderer>();
			mrProc.sharedMaterial = new Material(_matTemplate);
			mf.sharedMesh = LakiRouletteSectorMeshBuilder.BuildRingSectorMesh(rMin, rMax, a0, a1, _angularSmooth, pivotOffset);
			surfaceGo.transform.localPosition = surfaceLocalPos;
			return mrProc;
		}

		private bool HasAnyLakiRouletteCatalogPrefab()
		{
			if (_resolvedCatalog == null) return false;
			for (int t = 0; t < CombatAttackVisualCatalogSO.LakiRouletteTileTypes; t++)
			{
				var te = (RouletteArenaService.TileEffectType)t;
				if (_resolvedCatalog.TryGetLakiRouletteTilePrefab(true, te, out var p) && p != null) return true;
				if (_resolvedCatalog.TryGetLakiRouletteTilePrefab(false, te, out p) && p != null) return true;
			}
			return false;
		}

		private GameObject ResolveLakiRouletteTilePrefab(bool inner, RouletteArenaService.TileEffectType effectType)
		{
			if (_resolvedCatalog == null) return null;
			if (_resolvedCatalog.TryGetLakiRouletteTilePrefab(inner, effectType, out var prefab) && prefab != null)
				return prefab;
			if (_resolvedCatalog.TryGetLakiRouletteTilePrefab(inner, RouletteArenaService.TileEffectType.Neutral, out prefab) && prefab != null)
				return prefab;
			for (int t = 0; t < CombatAttackVisualCatalogSO.LakiRouletteTileTypes; t++)
			{
				var te = (RouletteArenaService.TileEffectType)t;
				if (_resolvedCatalog.TryGetLakiRouletteTilePrefab(inner, te, out prefab) && prefab != null)
					return prefab;
			}
			return null;
		}

		private static Color ReadRendererBaseColor(MeshRenderer mr)
		{
			if (mr == null) return Color.white;
			var mat = mr.sharedMaterial;
			if (mat == null) return Color.white;
			if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
			if (mat.HasProperty("_Color")) return mat.color;
			return Color.white;
		}

		private void CacheTileBaseColor(int tileIndex, MeshRenderer mr, bool usesCatalogPrefab, RouletteArenaService.TileEffectType type)
		{
			Color c;
			if (usesCatalogPrefab)
				c = ReadRendererBaseColor(mr);
			else
			{
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
			}
			while (_baseColors.Count <= tileIndex) _baseColors.Add(Color.clear);
			_baseColors[tileIndex] = c;
		}

		private void BuildTiles()
		{
			while (transform.childCount > 0)
				DestroyImmediate(transform.GetChild(0).gameObject);
			_renderers.Clear();
			_baseColors.Clear();
			_surfaceFromCatalog.Clear();

			int total = _sectorCount * _radialBands;
			_tileCanvases = new TileInfoCanvas[total];
			_tileRoots = new Transform[total];
			_lastVisualTileTypes = new RouletteArenaService.TileEffectType[total];
			for (int t = 0; t < total; t++)
				_lastVisualTileTypes[t] = RouletteArenaService.TileEffectType.Neutral;

			int tileIndex = 0;
			for (int s = 0; s < _sectorCount; s++)
			{
				for (int band = 0; band < _radialBands; band++)
				{
					ComputeTileLayoutForIndex(tileIndex, out int b, out float a0, out float a1, out float rMin, out float rMax, out _, out float midDeg, out Vector3 layoutCenter);
					Vector3 rootWorld = TileRootWorldFromLayout(layoutCenter);
					Vector3 meshPivotOffset = rootWorld - _centerWorld;

					GameObject go = new GameObject($"Tile_{tileIndex:D2}_S{s}_B{band}");
					go.transform.SetParent(transform, false);
					go.transform.position = rootWorld;
					go.transform.localRotation = Quaternion.identity;
					_tileRoots[tileIndex] = go.transform;

					var startType = RouletteArenaService.TileEffectType.Neutral;
					MeshRenderer mr = BuildTileSurface(go.transform, b, startType, a0, a1, rMin, rMax, meshPivotOffset, midDeg, out bool fromCatalog);
					_lastVisualTileTypes[tileIndex] = startType;

					_renderers.Add(mr);
					_surfaceFromCatalog.Add(fromCatalog);
					CacheTileBaseColor(tileIndex, mr, fromCatalog, startType);
					if (!fromCatalog)
						ApplyProceduralTileTint(mr, startType);
					_tileCanvases[tileIndex] = BuildTileCanvas(go.transform, b);
					tileIndex++;
				}
			}
		}

		/// <summary>
		/// Creates the world-space canvas for one tile. Slots are NOT created here –
		/// they are rebuilt dynamically by <see cref="RefreshTileCanvas"/> whenever effects change.
		/// band 0 = inner ring, band 1 = outer ring (affects VLG spacing).
		/// Child of <paramref name="tileTr"/>; after layout, <see cref="RectTransform.localEulerAngles"/> is forced to (90, 0, −80).
		/// </summary>
		private TileInfoCanvas BuildTileCanvas(Transform tileTr, int band)
		{
			float k = TileCanvasLayoutScale * TileCanvasSizeBoost;
			var canvasGO = new GameObject("TileInfoCanvas");
			canvasGO.transform.SetParent(tileTr, false);
			canvasGO.transform.localPosition = new Vector3(0f, _canvasHeightOffset, 0f);

			var canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.WorldSpace;
			var canvasRt = canvasGO.GetComponent<RectTransform>();
			canvasRt.sizeDelta = new Vector2(1400f * k, 900f * k);
			float s = (_canvasScale > 0f ? _canvasScale : 0.004f) * k;

			// Container anchored to canvas centre, shifted right (+X) to sit over the tile
			var containerGO = new GameObject("SlotsContainer");
			containerGO.transform.SetParent(canvasGO.transform, false);
			var containerRt = containerGO.AddComponent<RectTransform>();
			containerRt.anchorMin = new Vector2(0.5f, 0.5f);
			containerRt.anchorMax = new Vector2(0.5f, 0.5f);
			containerRt.pivot     = new Vector2(0.5f, 0.5f);
			containerRt.anchoredPosition = new Vector2(250f * k, 0f);
			containerRt.sizeDelta = new Vector2(1380f * k, 0f); // height driven by ContentSizeFitter

			var vlg = containerGO.AddComponent<VerticalLayoutGroup>();
			vlg.childAlignment        = TextAnchor.MiddleCenter;
			vlg.childControlWidth     = false;
			vlg.childControlHeight    = false;
			vlg.childForceExpandWidth = false;
			vlg.childForceExpandHeight= false;
			// Inner tiles (band 0) are narrower radially → more vertical spacing needed
			vlg.spacing = band == 0 ? 300f * k : 200f * k;

			var csf = containerGO.AddComponent<ContentSizeFitter>();
			csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
			csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

			canvasGO.transform.localScale = new Vector3(s, s, s);
			canvasRt.localEulerAngles = new Vector3(TileCanvasEulerX, TileCanvasEulerY, TileCanvasEulerZ);

			return new TileInfoCanvas { SlotsContainer = containerGO.transform };
		}

		/// <summary>
		/// Appends a single image+text row into the slots container.
		/// Icon is hidden (transparent) when <paramref name="icon"/> is null.
		/// </summary>
		private static void AppendSlotRow(Transform container, string label, Sprite icon)
		{
			float k = TileCanvasLayoutScale * TileCanvasSizeBoost;
			float rowW   = 1380f * k;
			float rowH   = 320f * k;
			float iconSz = 320f * k;
			float gap    = 180f * k;
			float pad    = 12f * k;

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
			int padI = Mathf.Max(0, Mathf.RoundToInt(pad));
			hlg.padding               = new RectOffset(padI, padI, 0, 0);

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
			tmp.fontSize           = 120f * k;
			tmp.color              = Color.black;
			tmp.alignment          = TextAlignmentOptions.MidlineLeft;
			tmp.enableAutoSizing   = false;
			tmp.enableWordWrapping = false;
			tmp.overflowMode       = TextOverflowModes.Ellipsis;
		}

		public void RefreshFrom(RouletteArenaService service)
		{
			if (service == null) return;
			CacheTileEffects(service);
			int tiles = service.TileCount;
			for (int i = 0; i < _renderers.Count && i < tiles; i++)
			{
				var type = service.GetTileEffect(i);
				if (_tileRoots != null && i < _tileRoots.Length && _tileRoots[i] != null
				    && (_lastVisualTileTypes == null || i >= _lastVisualTileTypes.Length || _lastVisualTileTypes[i] != type))
				{
					if (_suitLabels != null && i < _suitLabels.Length && _suitLabels[i] != null)
					{
						Destroy(_suitLabels[i].gameObject);
						_suitLabels[i] = null;
					}
					ComputeTileLayoutForIndex(i, out int band, out float a0, out float a1, out float rMin, out float rMax, out _, out float midDeg, out Vector3 layoutCenter);
					Vector3 rootWorld = TileRootWorldFromLayout(layoutCenter);
					_tileRoots[i].position = rootWorld;
					Vector3 meshPivotOffset = rootWorld - _centerWorld;
					var mr = BuildTileSurface(_tileRoots[i], band, type, a0, a1, rMin, rMax, meshPivotOffset, midDeg, out bool fromCatalog);
					_renderers[i] = mr;
					while (_surfaceFromCatalog.Count <= i) _surfaceFromCatalog.Add(false);
					_surfaceFromCatalog[i] = fromCatalog;
					if (_lastVisualTileTypes != null && i < _lastVisualTileTypes.Length)
						_lastVisualTileTypes[i] = type;
					CacheTileBaseColor(i, mr, fromCatalog, type);
					if (!fromCatalog)
						ApplyProceduralTileTint(mr, type);
				}
				else
				{
					bool fromCatalog = i < _surfaceFromCatalog.Count && _surfaceFromCatalog[i];
					if (!fromCatalog)
					{
						ApplyProceduralTileTint(_renderers[i], type);
						CacheTileBaseColor(i, _renderers[i], false, type);
					}
				}

				if (_tileCanvases != null && i < _tileCanvases.Length)
					RefreshTileCanvas(i, service.GetTileAssignedEffects(i));
			}
		}

		private void ApplyProceduralTileTint(MeshRenderer mr, RouletteArenaService.TileEffectType type)
		{
			if (mr == null) return;
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
			var mat = mr.sharedMaterial;
			if (mat != null)
			{
				if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
				else if (mat.HasProperty("_Color")) mat.color = c;
			}
		}

		/// <summary>
		/// Clears the tile's slot container and rebuilds one row per pre-assigned effect.
		/// The VLG+ContentSizeFitter centres them vertically regardless of count.
		/// </summary>
		private void RefreshTileCanvas(int i, Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect[] effects)
		{
			var container = _tileCanvases[i].SlotsContainer;
			if (container == null) return;

			// Remove previous slots
			for (int c = container.childCount - 1; c >= 0; c--)
				Destroy(container.GetChild(c).gameObject);

			if (effects != null)
				foreach (var e in effects)
					if (e != null)
						AppendSlotRow(container, string.IsNullOrEmpty(e.Name) ? "" : e.Name, e.TileIcon);

			UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
				container.GetComponent<RectTransform>());
		}

		public void SetEmphasis(System.Collections.Generic.ICollection<int> tileIndices, float t01, float extraIntensity = 0.75f)
		{
			if (tileIndices == null || _renderers.Count == 0) return;
			float k = Mathf.Clamp01(t01);
			for (int i = 0; i < _renderers.Count; i++)
			{
				if (_renderers[i] == null) continue;
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

			float split = RouletteArenaService.ComputeSplitRadius(_innerRadius, _outerRadius);
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
			float halfGap = RouletteArenaService.ComputeTileAngularHalfGapDeg(_arcDeg, _sectorCount);

			int sector = tileIndex / _radialBands;
			int band = tileIndex % _radialBands;
			float a0 = _arcStartDeg + sector * sectorAngle + halfGap;
			float a1 = _arcStartDeg + (sector + 1) * sectorAngle - halfGap;
			float amidDeg = 0.5f * (a0 + a1);
			float amid = amidDeg * Mathf.Deg2Rad;

			RouletteArenaService.ComputeBandRadialExtents(
				_innerRadius, _outerRadius, band, _radialGap, out float rMin, out float rMax);
			float rMid = 0.5f * (rMin + rMax);

			float cx = _centerWorld.x + Mathf.Cos(amid) * rMid;
			float cz = _centerWorld.z + Mathf.Sin(amid) * rMid;
			var layoutCenter = new Vector3(cx, _centerWorld.y, cz);
			return TileRootWorldFromLayout(layoutCenter);
		}
	}
}


