using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core;
using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Shared;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.Services.UpdateService;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.AudioService;
using Object = UnityEngine.Object;
using Logic.Scripts.GameDomain.MVC.Boss.Telegraph;
using Assets.Logic.Scripts.GameDomain.Effects;

namespace Logic.Scripts.GameDomain.MVC.Boss.Attacks.Feather
{
    public class FeatherLinesHandler : IBossAttackHandler, IUpdatable, Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility
    {
        private readonly FeatherLinesParams _params;
        private readonly IUpdateSubscriptionService _updateSvc;

		private class FeatherSubView
        {
            public GameObject StripRoot;
        }

        private FeatherSubView[] _views;
        private int _specialIndex;
        private bool _isPushMode = true;
        private bool? _ctorIsPush = null;

        private LineRenderer _singleArrow;
        private Vector3 _sStart, _sEnd, _axisUnit;
        private float _stripWidth;
		private readonly Material _baseMaterial;
		private readonly Material _displacementMaterial;
		private readonly Material _lineBase;
		private readonly Material _lineDisp;
		private readonly Material _meshBase;
		private readonly Material _meshDisp;

        private Transform _parentTransform;
        private ArenaPosReference _arenaRef;
        private INaraController _naraController;

		private static bool? _nextTelegraphDisplacementEnabled = null;
		private bool _telegraphDisplacementEnabled = true;

        private static bool? _nextTelegraphPushMode = null;
        private static bool _globalFallbackPushMode = true;
        private static Func<bool> _isPushProvider = null;

		public static void PrimeNextTelegraphDisplacementEnabled(bool enabled) => _nextTelegraphDisplacementEnabled = enabled;
        public static void PrimeNextTelegraphPushMode(bool isPush) => _nextTelegraphPushMode = isPush;
        public static void SetGlobalFallbackPushMode(bool isPushAsDefault) => _globalFallbackPushMode = isPushAsDefault;
        public static void ConfigurePushProvider(Func<bool> provider) => _isPushProvider = provider;

        public static Vector3 CurrentSpecialStart;
        public static Vector3 CurrentSpecialEnd;
        public static Vector3 CurrentSpecialAxis;
        public static float CurrentStripWidth;

        private int _layerId = -1;
        private float _yOffset = 0.05f;
        private int _rqAdd = 0;
        private IAudioService _audio;

        /// <summary>World-space half-length of feather strips from arena center (full span = 2×). When unset (0) in old data, uses 10m as a sane arena default.</summary>
        private float StripHalfExtent => Mathf.Max(0.1f, _params.stripHalfExtent > 1e-4f ? _params.stripHalfExtent : 10f);

        /// <summary>Perpendicular spacing between strips uses this half-extent (wider than strip length) so lines cover more of the arena.</summary>
        private float StripSpreadHalfExtent => StripHalfExtent * 1.4f;

        private float StripTelegraphUniformScale =>
            _params.telegraphStripUniformScale > 1e-4f ? _params.telegraphStripUniformScale : 1f;

        /// <summary>Strip mesh: local +Z along the faixa. X / Z / XZ usam (0.3,1,1) — mesmo local após rotação por eixo.</summary>
        private const float FeatherStripNarrowAxisScale = 0.5f;

        private Vector3 TelegraphStripLocalScale(float diagonalUniformScale)
        {
            if (_params.axisMode == FeatherAxisMode.Diagonal)
                return new Vector3(diagonalUniformScale, 1f, diagonalUniformScale);
            return new Vector3(FeatherStripNarrowAxisScale, 1f, 1f);
        }

        private static float StripSpreadCoordinate(int index, int count, float halfExtent)
        {
            if (count <= 1) return 0f;
            float t = index / (float)(count - 1);
            return Mathf.Lerp(-halfExtent, halfExtent, t);
        }

        private void GetStripEndpoints(int i, int n, Vector3 center, out Vector3 start, out Vector3 end)
        {
            float y = center.y;
            float h = StripHalfExtent;
            float spreadH = StripSpreadHalfExtent;

            switch (_params.axisMode)
            {
                case FeatherAxisMode.X:
                {
                    // Strips run along ±X; same arena X for all, spread evenly in Z.
                    float xCenter = center.x;
                    float z = center.z + StripSpreadCoordinate(i, n, spreadH);
                    start = new Vector3(xCenter - h, y, z);
                    end = new Vector3(xCenter + h, y, z);
                    break;
                }
                case FeatherAxisMode.Z:
                {
                    // Strips run along ±Z; same arena Z for all, spread evenly in X.
                    float zCenter = center.z;
                    float x = center.x + StripSpreadCoordinate(i, n, spreadH);
                    start = new Vector3(x, y, zCenter - h);
                    end = new Vector3(x, y, zCenter + h);
                    break;
                }
                case FeatherAxisMode.XZ:
                {
                    int nAlongX = (n + 1) / 2;
                    int nAlongZ = n / 2;
                    if ((i % 2) == 0)
                    {
                        int k = i / 2;
                        float xCenter = center.x;
                        float z = center.z + StripSpreadCoordinate(k, nAlongX, spreadH);
                        start = new Vector3(xCenter - h, y, z);
                        end = new Vector3(xCenter + h, y, z);
                    }
                    else
                    {
                        int k = (i - 1) / 2;
                        float zCenter = center.z;
                        float x = center.x + StripSpreadCoordinate(k, nAlongZ, spreadH);
                        start = new Vector3(x, y, zCenter - h);
                        end = new Vector3(x, y, zCenter + h);
                    }
                    break;
                }
                case FeatherAxisMode.Diagonal:
                default:
                {
                    Vector3 d = new Vector3(1f, 0f, 1f).normalized;
                    float along = StripSpreadCoordinate(i, n, spreadH);
                    Vector3 mid = new Vector3(center.x, y, center.z) + d * along;
                    start = mid - d * h;
                    end = mid + d * h;
                    break;
                }
            }
        }

        /// <summary>Lateral unit (XZ) used by strip width, knockback side, and DisplacementEffect — Cross(tangent, world up).</summary>
        private static Vector3 FeatherStripLateralNormal(Vector3 stripA, Vector3 stripB)
        {
            Vector3 ab = stripB - stripA;
            ab.y = 0f;
            if (ab.sqrMagnitude < 1e-8f) return Vector3.right;
            Vector3 tangent = ab.normalized;
            Vector3 n = Vector3.Cross(tangent, Vector3.up);
            n.y = 0f;
            return n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.right;
        }

        private void RefreshSpecialStripGlobals(Vector3 arenaCenter, int n)
        {
            if (_specialIndex < 0 || _specialIndex >= n || n <= 0) return;
            GetStripEndpoints(_specialIndex, n, arenaCenter, out _sStart, out _sEnd);
            _axisUnit = _sEnd - _sStart;
            _axisUnit.y = 0f;
            if (_axisUnit.sqrMagnitude > 1e-8f) _axisUnit.Normalize();
            else _axisUnit = Vector3.zero;
            CurrentSpecialStart = _sStart;
            CurrentSpecialEnd = _sEnd;
            CurrentSpecialAxis = _axisUnit;
        }

        private static void PatchDisplacementEffectsForFeatherStrip(
            List<AbilityEffect> effects,
            Vector3 stripA,
            Vector3 stripB,
            Vector3 playerWorld,
            bool isPushMode)
        {
            if (effects == null) return;
            Vector3 ab = stripB - stripA;
            ab.y = 0f;
            if (ab.sqrMagnitude < 1e-8f) return;
            Vector3 normal = FeatherStripLateralNormal(stripA, stripB);
            float t = Mathf.Clamp01(Vector3.Dot(playerWorld - stripA, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            Vector3 closest = stripA + ab * t;
            Vector3 toPlayer = playerWorld - closest;
            toPlayer.y = 0f;
            float side = Mathf.Sign(Vector3.Dot(normal, toPlayer));
            if (Mathf.Abs(side) < 1e-6f) side = 1f;
            Vector3 dir = side * normal;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] is DisplacementEffect disp)
                {
                    disp.direction = dir;
                    disp.isPush = isPushMode;
                }
            }
        }

        private static void BumpRenderQueueOnHierarchy(GameObject root, int add)
        {
            if (root == null || add == 0) return;
            var mrs = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int mi = 0; mi < mrs.Length; mi++)
            {
                if (mrs[mi] == null) continue;
                var m = mrs[mi].material;
                if (m != null) m.renderQueue += add;
            }
        }

        private GameObject _columnPrefabNormal;
        private GameObject _columnPrefabPull;
        private GameObject _columnPrefabPush;
        private GameObject _featherRowTelegraphNormal;
        private GameObject _featherRowTelegraphPull;
        private GameObject _featherRowTelegraphPush;

        public FeatherLinesHandler(FeatherLinesParams p, IUpdateSubscriptionService updateSubscriptionService)
        {
            _params = p; _updateSvc = updateSubscriptionService;
        }
        public FeatherLinesHandler(FeatherLinesParams p)
        {
            _params = p; _updateSvc = TryFindUpdateServiceInScene();
        }

		public FeatherLinesHandler(FeatherLinesParams p, bool isPull, IUpdateSubscriptionService updateSubscriptionService, Material baseMaterial = null, Material displacementMaterial = null)
        {
            _params = p;
            _updateSvc = updateSubscriptionService;
            _ctorIsPush = !isPull;
			_baseMaterial = baseMaterial;
			_displacementMaterial = displacementMaterial;
			_lineBase = baseMaterial;
			_lineDisp = displacementMaterial;
			_meshBase = baseMaterial;
			_meshDisp = displacementMaterial;
        }

		public FeatherLinesHandler(FeatherLinesParams p, bool isPull)
        {
            _params = p;
            _updateSvc = TryFindUpdateServiceInScene();
            _ctorIsPush = !isPull;
			_baseMaterial = null;
			_displacementMaterial = null;
			_lineBase = null;
			_lineDisp = null;
			_meshBase = null;
			_meshDisp = null;
        }

		public FeatherLinesHandler(FeatherLinesParams p, bool isPull, Material baseMaterial, Material displacementMaterial = null)
		{
			_params = p;
			_updateSvc = TryFindUpdateServiceInScene();
			_ctorIsPush = !isPull;
			_baseMaterial = baseMaterial;
			_displacementMaterial = displacementMaterial;
			_lineBase = baseMaterial;
			_lineDisp = displacementMaterial;
			_meshBase = baseMaterial;
			_meshDisp = displacementMaterial;
		}

		public FeatherLinesHandler(FeatherLinesParams p, bool isPull, Material lineBase, Material lineDisp, Material meshBase, Material meshDisp,
			GameObject columnNormalPrefab = null, GameObject columnPullPrefab = null, GameObject columnPushPrefab = null,
			GameObject featherRowTelegraphNormal = null, GameObject featherRowTelegraphPull = null, GameObject featherRowTelegraphPush = null)
		{
			_params = p;
			_updateSvc = TryFindUpdateServiceInScene();
			_ctorIsPush = !isPull;
			_baseMaterial = null;
			_displacementMaterial = null;
			_lineBase = lineBase;
			_lineDisp = lineDisp;
			_meshBase = meshBase;
			_meshDisp = meshDisp;
			_columnPrefabNormal = columnNormalPrefab;
			_columnPrefabPull = columnPullPrefab;
			_columnPrefabPush = columnPushPrefab;
			_featherRowTelegraphNormal = featherRowTelegraphNormal;
			_featherRowTelegraphPull = featherRowTelegraphPull;
			_featherRowTelegraphPush = featherRowTelegraphPush;
		}

        public void SetAudio(IAudioService audio) { _audio = audio; }

        private IUpdateSubscriptionService TryFindUpdateServiceInScene()
        {
            try
            {
                var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var go in all)
                    if (go.TryGetComponent<IUpdateSubscriptionService>(out var svc)) return svc;
            }
            catch { }
            return null;
        }

        public void PrepareTelegraph(Transform parentTransform)
        {
            _parentTransform = parentTransform;
            _arenaRef = Object.FindFirstObjectByType<ArenaPosReference>(FindObjectsInactive.Exclude);

            // Layering for unified Y/queue with fill
            var layering = TelegraphLayeringLocator.Service;
            if (layering != null)
            {
                var layer = layering.Register(preferTop: _telegraphDisplacementEnabled);
                _layerId = layer.Id;
                _yOffset = layer.Y;
                _rqAdd = layer.QueueAdd;
            }

			if (_nextTelegraphDisplacementEnabled.HasValue)
			{
				_telegraphDisplacementEnabled = _nextTelegraphDisplacementEnabled.Value;
				_nextTelegraphDisplacementEnabled = null;
			}

            _isPushMode = ResolveInitialPushMode();

            _naraController = _arenaRef != null ? _arenaRef.NaraController : null;

            int n = Mathf.Max(1, _params.featherCount);
			_views = new FeatherSubView[n];
			_specialIndex = _telegraphDisplacementEnabled ? UnityEngine.Random.Range(0, n) : -1;

            for (int i = 0; i < n; i++)
                _views[i] = new FeatherSubView();

			if (_telegraphDisplacementEnabled)
			{
				var arrowGO = new GameObject("FeatherDirectionArrow_Global");
				arrowGO.transform.SetParent(parentTransform, false);
				_singleArrow = arrowGO.AddComponent<LineRenderer>();
				var shader = Shader.Find("Universal Render Pipeline/Unlit");
				if (shader == null) shader = Shader.Find("Unlit/Color");
				if (shader == null) shader = Shader.Find("Sprites/Default");
				var arrowMat = new Material(shader);
				// não forçar renderQueue; manter padrão opaco do shader
				// cor da seta igual à cor do material ativo (displacement => displacementMaterial; senão baseMaterial)
				Material refMat = (_telegraphDisplacementEnabled && (_meshDisp != null || _lineDisp != null))
					? (_meshDisp != null ? _meshDisp : _lineDisp)
					: (_meshBase != null ? _meshBase : (_lineBase != null ? _lineBase : _baseMaterial));
				if (refMat != null)
				{
					Color col;
					if (refMat.HasProperty("_BaseColor")) col = refMat.GetColor("_BaseColor");
					else if (refMat.HasProperty("_Color")) col = refMat.color;
					else col = Color.white;
					col.a = 1f; // força opacidade total na seta
					if (arrowMat.HasProperty("_BaseColor")) arrowMat.SetColor("_BaseColor", col);
					if (arrowMat.HasProperty("_Color")) arrowMat.color = col;
				}
				_singleArrow.material = arrowMat;
				_singleArrow.useWorldSpace = true;
				_singleArrow.loop = false;
				_singleArrow.widthMultiplier = 0.08f;
				_singleArrow.enabled = true;
			}

            UpdateTelegraphGeometryAtCenter(parentTransform.position);
			// Apenas o ataque habilitado para deslocamento deve fixar eixo/posições globais
			if (_telegraphDisplacementEnabled)
			{
				FreezeSpecialAxis(parentTransform.position);
			}
            EnsureAudio();
            _audio?.PlaySfx(SfxIds.Hocari_Ataque_Cortes, AudioChannelType.SfxBoss);
            _updateSvc?.RegisterUpdatable(this);

            // Start hidden; boss controller will reveal at mid prep
            SetTelegraphVisible(false);
            Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.Register(this);
        }

        private bool ResolveInitialPushMode()
        {
            if (_ctorIsPush.HasValue) return _ctorIsPush.Value;
            if (_nextTelegraphPushMode.HasValue)
            {
                bool v = _nextTelegraphPushMode.Value; _nextTelegraphPushMode = null; return v;
            }
            if (_isPushProvider != null) { try { return _isPushProvider(); } catch { } }
            if (TryInferPushFromParamsViaReflection(out bool fromParams)) return fromParams;
            return _globalFallbackPushMode;
        }

        private bool TryInferPushFromParamsViaReflection(out bool isPush)
        {
            isPush = _isPushMode;
            try
            {
                var t = _params.GetType();
                foreach (var name in new[] { "isPush", "push", "shouldPush", "knockback" })
                {
                    var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null && f.FieldType == typeof(bool)) { isPush = (bool)f.GetValue(_params); return true; }
                    var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null && p.PropertyType == typeof(bool)) { isPush = (bool)p.GetValue(_params); return true; }
                }
                var enumField = t.GetField("mode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              ?? t.GetField("displacementMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (enumField != null)
                {
                    object val = enumField.GetValue(_params);
                    if (val != null)
                    {
                        string s = val.ToString().ToLowerInvariant();
                        if (s.Contains("push") || s.Contains("knockback")) { isPush = true; return true; }
                        if (s.Contains("pull") || s.Contains("grapple")) { isPush = false; return true; }
                    }
                }
            }
            catch { }
            return false;
        }

        private GameObject ResolveColumnPrefabForIndex(int i)
        {
            GameObject col = null;
            bool anyColumn = _columnPrefabNormal != null || _columnPrefabPull != null || _columnPrefabPush != null;
            if (anyColumn)
            {
                if (_telegraphDisplacementEnabled && i == _specialIndex)
                {
                    if (_isPushMode)
                        col = _columnPrefabPush != null ? _columnPrefabPush : _columnPrefabNormal;
                    else
                        col = _columnPrefabPull != null ? _columnPrefabPull : _columnPrefabNormal;
                }
                else
                    col = _columnPrefabNormal;
            }
            if (col != null) return col;

            bool anyRow = _featherRowTelegraphNormal != null || _featherRowTelegraphPull != null || _featherRowTelegraphPush != null;
            if (!anyRow) return null;

            if (_telegraphDisplacementEnabled && i == _specialIndex)
            {
                if (_isPushMode)
                    return _featherRowTelegraphPush != null ? _featherRowTelegraphPush : _featherRowTelegraphNormal;
                return _featherRowTelegraphPull != null ? _featherRowTelegraphPull : _featherRowTelegraphNormal;
            }
            return _featherRowTelegraphNormal;
        }

        private void UpdateTelegraphGeometryAtCenter(Vector3 center)
        {
            int n = _views.Length;
            float visY = center.y + _yOffset;

            for (int i = 0; i < n; i++)
            {
                GetStripEndpoints(i, n, center, out Vector3 start, out Vector3 end);

                GameObject colPrefab = ResolveColumnPrefabForIndex(i);
                if (_views[i].StripRoot != null)
                {
                    Object.Destroy(_views[i].StripRoot);
                    _views[i].StripRoot = null;
                }

                if (colPrefab == null)
                {
                    Debug.LogWarning(
                        "[FeatherLinesHandler] No feather telegraph prefab resolved — assign Feather Lines / Feather Columns on CombatAttackVisualCatalogSO.");
                    continue;
                }

                _views[i].StripRoot = Object.Instantiate(colPrefab, _parentTransform, false);
                Vector3 a = new Vector3(start.x, visY, start.z);
                Vector3 b = new Vector3(end.x, visY, end.z);
                Vector3 ab = b - a;
                float len = ab.magnitude;
                Vector3 hdir = len > 1e-6f ? new Vector3(ab.x, 0f, ab.z).normalized : Vector3.forward;
                Vector3 mid = (a + b) * 0.5f;
                var ct = _views[i].StripRoot.transform;
                ct.SetPositionAndRotation(mid, Quaternion.FromToRotation(Vector3.forward, hdir));
                ct.localScale = TelegraphStripLocalScale(StripTelegraphUniformScale);
                BumpRenderQueueOnHierarchy(_views[i].StripRoot, _rqAdd);
            }
        }

		private void FreezeSpecialAxis(Vector3 center)
        {
			// Se o telegraph de deslocamento estiver desabilitado ou não há faixa especial, não fixe eixo global
			if (!_telegraphDisplacementEnabled) return;
			if (_views == null || _views.Length == 0) return;
			if (_specialIndex < 0 || _specialIndex >= _views.Length) return;

            int n = _views.Length;
            GetStripEndpoints(_specialIndex, n, center, out _sStart, out _sEnd);

            _axisUnit = (_sEnd - _sStart);
            _axisUnit.y = 0f;
            if (_axisUnit.sqrMagnitude > 1e-8f) _axisUnit.Normalize();
            _stripWidth = _params.width;

            CurrentSpecialStart = _sStart;
            CurrentSpecialEnd = _sEnd;
            CurrentSpecialAxis = _axisUnit;
            CurrentStripWidth = _stripWidth;

			var playerWorld = ResolvePlayerWorldPosition();
			if (_telegraphDisplacementEnabled && _singleArrow != null) UpdateSingleArrow(playerWorld);
        }

        private Vector3 ResolvePlayerWorldPosition()
        {
            if (_arenaRef != null)
                return _arenaRef.RelativeArenaPositionToRealPosition(_arenaRef.GetPlayerArenaPosition());

            var naraView = Object.FindFirstObjectByType<Nara.NaraView>(FindObjectsInactive.Exclude);
            if (naraView != null) return naraView.transform.position;

            return Vector3.zero;
        }

        public bool ComputeHits(ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
        {
            Vector3 center = arenaReference != null ? arenaReference.transform.position : originTransform.position;
            Vector3 playerWorld = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
            int n = _views.Length;

            for (int i = 0; i < n; i++)
            {
                GetStripEndpoints(i, n, center, out Vector3 start, out Vector3 end);
                Vector3[] verts = StripMath.GenerateStripVertices(start, end, _params.width);
                if (PointInQuad(playerWorld, verts)) return true;
            }
            return false;
        }

        private bool PointInQuad(Vector3 p, Vector3[] q)
        {
            Vector2 P = new Vector2(p.x, p.z);
            Vector2 A = new Vector2(q[0].x, q[0].z);
            Vector2 B = new Vector2(q[1].x, q[1].z);
            Vector2 C = new Vector2(q[2].x, q[2].z);
            Vector2 D = new Vector2(q[3].x, q[3].z);
            return PointInTriangle(P, A, B, C) || PointInTriangle(P, A, C, D);
        }

        private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s = a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y;
            float t = a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y;
            if ((s < 0) != (t < 0)) return false;
            float A = -b.y * c.x + a.y * (c.x - b.x) + a.x * (b.y - c.y) + b.x * c.y;
            if (A < 0.0f) { s = -s; t = -t; A = -A; }
            return s > 0 && t > 0 && (s + t) < A;
        }

        private float PerpendicularDistanceToLineXZ(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector2 P = new Vector2(point.x, point.z);
            Vector2 A = new Vector2(a.x, a.z);
            Vector2 B = new Vector2(b.x, b.z);
            Vector2 AB = B - A;
            float len = AB.magnitude;
            if (len < 1e-6f) return Vector2.Distance(P, A);
            float area2 = Mathf.Abs((B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x));
            return area2 / len;
        }

        private void EnsureAudio()
        {
            if (_audio != null) return;
            try
            {
                var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var mb in behaviours)
                    if (mb is IAudioService a) { _audio = a; break; }
            }
            catch { }
        }

        public IEnumerator ExecuteEffects(List<AbilityEffect> effects, ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
        {
            if (effects == null || effects.Count == 0) yield break;
            IEffectable target = arenaReference.NaraController as IEffectable;
            if (target == null) yield break;

            Vector3 center = arenaReference != null ? arenaReference.transform.position : originTransform.position;
            int n = _views != null ? _views.Length : 0;

            RefreshSpecialStripGlobals(center, n);

            var sStrip = (_views != null && _specialIndex >= 0 && _specialIndex < _views.Length) ? _views[_specialIndex].StripRoot : null;
            if (sStrip != null) sStrip.SetActive(false);

            yield return new WaitForSeconds(0.5f);

            EnsureAudio();
            _audio?.PlaySfx(SfxIds.Hocari_Orbe, AudioChannelType.SfxBoss);

            int lastIndex = effects.Count - 1;
            Vector3 playerWorld = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
            PatchDisplacementEffectsForFeatherStrip(effects, _sStart, _sEnd, playerWorld, _isPushMode);
            if (lastIndex >= 0 && StripMath.IsPointInsideStrip(_sStart, _sEnd, _stripWidth, playerWorld))
            {
                for (int ei = 0; ei < lastIndex; ei++)
                {
                    AbilityEffect fx = effects[ei];
                    fx?.Execute(caster, target);
                }
            }

            yield return new WaitForSeconds(0.5f);

            if (effects.Count > 0)
            {
                AbilityEffect fx1 = effects[lastIndex];
                if (fx1 != null)
                {
                    if (fx1 is IForceScaledEffect scalable1)
                    {
                        int stacks = GetDebuffStacks();
                        float distMeters = PerpendicularDistanceToLineXZ(
                            arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition()),
                            _sStart, _sEnd);
                        int distMul = Mathf.RoundToInt(distMeters);
                        scalable1.SetForceScalers(stacks, distMul);
                    }

                    if (fx1 is IAsyncEffect asyncFx1)
                        yield return asyncFx1.ExecuteRoutine(caster, target);
                    else
                        fx1.Execute(caster, target);

                    yield return new WaitForSeconds(0.5f);
                }
            }

            for (int i = 0; i < n && effects.Count > 0; i++)
            {
                if (i == _specialIndex) continue;

                GetStripEndpoints(i, n, center, out Vector3 start, out Vector3 end);

                Vector3 playerWorld2 = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
                if (StripMath.IsPointInsideStrip(start, end, _params.width, playerWorld2))
                {
                    int lastIndex2 = effects.Count - 1;
                    for (int ei = 0; ei < lastIndex2; ei++)
                    {
                        AbilityEffect fx = effects[ei];
                        if (fx is IForceScaledEffect scalable)
                        {
                            int stacks2 = GetDebuffStacks();
                            float distMeters2 = PerpendicularDistanceToLineXZ(
                                arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition()),
                                _sStart, _sEnd);
                            int distMul2 = Mathf.RoundToInt(distMeters2);
                            scalable.SetForceScalers(stacks2, distMul2);
                        }
                        fx?.Execute(caster, target);
                    }
                }
            }
        }

        public void ManagedUpdate()
        {
			if (_singleArrow == null) return;
            if (_views == null || _views.Length == 0) return;
            if (_axisUnit.sqrMagnitude < 1e-8f) return;

            var playerWorld = ResolvePlayerWorldPosition();
			if (_telegraphDisplacementEnabled) UpdateSingleArrow(playerWorld);
        }

        private int GetDebuffStacks()
        {
            if (_arenaRef == null) return 0;
            var ic = _arenaRef.NaraController;
            if (ic is NaraController concrete) return concrete.GetNumberDebuffs();
            return 0;
        }

		private void UpdateSingleArrow(Vector3 playerWorld)
        {
			if (_singleArrow == null) return;
            Vector3 v = playerWorld - _sStart; v.y = 0f;
            float t = Vector3.Dot(v, _axisUnit);
            Vector3 proj = _sStart + _axisUnit * t; proj.y = playerWorld.y;

            Vector3 perp = proj - new Vector3(playerWorld.x, playerWorld.y, playerWorld.z);
            perp.y = 0f;
            if (perp.sqrMagnitude < 1e-6f) perp = new Vector3(-_axisUnit.z, 0f, _axisUnit.x);

            Vector3 dir = (_isPushMode ? -perp : perp).normalized;

            float y = 0.3f;
            float outOffset = Mathf.Max(0.35f, _stripWidth * 0.5f + 0.15f);
            float shaftLen = Mathf.Max(0.75f, _stripWidth * 0.9f);
            float headLen = shaftLen * 0.35f;
            float headHalfW = headLen * 0.6f;

            Vector3 origin = new Vector3(playerWorld.x, y, playerWorld.z) + dir * outOffset;
            Vector3 tip = origin + dir * shaftLen;
            Vector3 tail = origin - dir * 0.25f;

            Vector3 side = new Vector3(-dir.z, 0f, dir.x);
            Vector3 leftWing = tip - dir * headLen + side * headHalfW;
            Vector3 rightWing = tip - dir * headLen - side * headHalfW;

            _singleArrow.enabled = true;
            _singleArrow.positionCount = 5;
            _singleArrow.SetPosition(0, tail);
            _singleArrow.SetPosition(1, tip);
            _singleArrow.SetPosition(2, leftWing);
            _singleArrow.SetPosition(3, tip);
            _singleArrow.SetPosition(4, rightWing);
        }

        public void Cleanup()
        {
            _updateSvc?.UnregisterUpdatable(this);

            if (_views != null)
            {
                for (int i = 0; i < _views.Length; i++)
                {
                    if (_views[i]?.StripRoot != null)
                    {
                        Object.Destroy(_views[i].StripRoot);
                        _views[i].StripRoot = null;
                    }
                }
            }

            if (_singleArrow != null)
            {
                Object.Destroy(_singleArrow.gameObject);
                _singleArrow = null;
            }

            _views = null;
            Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.Unregister(this);
        }

        public void SetTelegraphVisible(bool visible)
        {
            if (_views != null)
            {
                for (int i = 0; i < _views.Length; i++)
                {
                    if (_views[i]?.StripRoot != null) _views[i].StripRoot.SetActive(visible);
                }
            }
            if (_singleArrow != null) _singleArrow.enabled = visible;
        }
    }
}
