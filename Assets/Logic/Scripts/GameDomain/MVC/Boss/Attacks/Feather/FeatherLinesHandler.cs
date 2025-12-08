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

namespace Logic.Scripts.GameDomain.MVC.Boss.Attacks.Feather
{
    public class FeatherLinesHandler : IBossAttackHandler, IUpdatable, Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility
    {
        private readonly FeatherLinesParams _params;
        private readonly IUpdateSubscriptionService _updateSvc;

		private class FeatherSubView
        {
            public LineRenderer Line;
            public MeshFilter MeshFilter;
            public MeshRenderer MeshRenderer;
            public Mesh Mesh;
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

		public FeatherLinesHandler(FeatherLinesParams p, bool isPull, Material lineBase, Material lineDisp, Material meshBase, Material meshDisp)
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
            {
                GameObject go = new GameObject("FeatherSubActionView");
                go.transform.SetParent(parentTransform, false);

                var v = new FeatherSubView
                {
                    Line = go.AddComponent<LineRenderer>(),
                    MeshFilter = go.AddComponent<MeshFilter>(),
                    MeshRenderer = go.AddComponent<MeshRenderer>(),
                    Mesh = new Mesh { name = "FeatherStripMesh" }
                };

                Material selLine = (_telegraphDisplacementEnabled && i == _specialIndex && _lineDisp != null)
					? _lineDisp
					: (_lineBase != null ? _lineBase : (_baseMaterial != null ? _baseMaterial : new Material(Shader.Find("Sprites/Default"))));
				var lineMat = new Material(selLine);
				lineMat.renderQueue += _rqAdd;
				v.Line.material = lineMat;
                v.Line.useWorldSpace = true;
                v.Line.loop = true;
                v.Line.widthMultiplier = 0.1f;
				// Cor via material (ShaderGraph); removemos tint manual

				Material selMesh = (_telegraphDisplacementEnabled && i == _specialIndex && _meshDisp != null)
					? _meshDisp
					: (_meshBase != null ? _meshBase : (_baseMaterial != null ? _baseMaterial : new Material(Shader.Find("Sprites/Default"))));
				var meshMat = new Material(selMesh);
				meshMat.renderQueue += _rqAdd;
				v.MeshRenderer.material = meshMat;
                v.MeshFilter.sharedMesh = v.Mesh;

                _views[i] = v;
            }

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
            _audio?.PlayAudio(AudioClipType.BossPrepAttack1SFX, AudioChannelType.Fx, AudioPlayType.OneShot);
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

        private void UpdateTelegraphGeometryAtCenter(Vector3 center)
        {
            float spacing = Mathf.Max(0.1f, _params.margin);
            int n = _views.Length;

            for (int i = 0; i < n; i++)
            {
                Vector3 start, end;
                switch (_params.axisMode)
                {
                    case FeatherAxisMode.X:
                    {
                        float offset = (i - (n - 1) * 0.5f) * spacing;
                        start = new Vector3(center.x - 100f, center.y, center.z + offset);
                        end = new Vector3(center.x + 100f, center.y, center.z + offset);
                        break;
                    }
                    case FeatherAxisMode.Z:
                    {
                        float offset = (i - (n - 1) * 0.5f) * spacing;
                        start = new Vector3(center.x + offset, center.y, center.z - 100f);
                        end = new Vector3(center.x + offset, center.y, center.z + 100f);
                        break;
                    }
                    case FeatherAxisMode.XZ:
                    {
                        int nX = (n + 1) / 2;
                        int nZ = n / 2;
                        if ((i % 2) == 0)
                        {
                            int k = i / 2;
                            float offX = (k - (nX - 1) * 0.5f) * spacing;
                            start = new Vector3(center.x - 100f, center.y, center.z + offX);
                            end   = new Vector3(center.x + 100f, center.y, center.z + offX);
                        }
                        else
                        {
                            int k = (i - 1) / 2;
                            float offZ = (k - (nZ - 1) * 0.5f) * spacing;
                            start = new Vector3(center.x + offZ, center.y, center.z - 100f);
                            end   = new Vector3(center.x + offZ, center.y, center.z + 100f);
                        }
                        break;
                    }
                    case FeatherAxisMode.Diagonal:
                    default:
                    {
                        float offset = (i - (n - 1) * 0.5f) * spacing;
                        start = new Vector3(center.x - 100f, center.y, center.z - 100f + offset);
                        end   = new Vector3(center.x + 100f, center.y, center.z + 100f + offset);
                        break;
                    }
                }

                Vector3[] vertsWorld = StripMath.GenerateStripVertices(start, end, _params.width);
                for (int p = 0; p < vertsWorld.Length; p++) vertsWorld[p].y = _yOffset;

                _views[i].Line.positionCount = vertsWorld.Length;
                _views[i].Line.SetPositions(vertsWorld);

                Transform mT = _views[i].MeshFilter.transform;
                mT.localPosition = new Vector3(0f, _yOffset, 0f);
                mT.localRotation = Quaternion.identity;

                Vector3 v0L = mT.InverseTransformPoint(vertsWorld[0]);
                Vector3 v1L = mT.InverseTransformPoint(vertsWorld[1]);
                Vector3 v2L = mT.InverseTransformPoint(vertsWorld[2]);
                Vector3 v3L = mT.InverseTransformPoint(vertsWorld[3]);

                _views[i].Mesh.Clear();
                _views[i].Mesh.vertices = new Vector3[] { v0L, v1L, v2L, v3L };
                _views[i].Mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
                _views[i].Mesh.RecalculateNormals();
                _views[i].Mesh.RecalculateBounds();
            }
        }

		private void FreezeSpecialAxis(Vector3 center)
        {
			// Se o telegraph de deslocamento estiver desabilitado ou não há faixa especial, não fixe eixo global
			if (!_telegraphDisplacementEnabled) return;
			if (_views == null || _views.Length == 0) return;
			if (_specialIndex < 0 || _specialIndex >= _views.Length) return;

            float spacing = Mathf.Max(0.1f, _params.margin);
            float specialOffset = (_specialIndex - (_views.Length - 1) * 0.5f) * spacing;

            if (_params.axisMode == FeatherAxisMode.X)
            {
                _sStart = new Vector3(center.x - 100f, center.y, center.z + specialOffset);
                _sEnd   = new Vector3(center.x + 100f, center.y, center.z + specialOffset);
            }
            else if (_params.axisMode == FeatherAxisMode.Z)
            {
                _sStart = new Vector3(center.x + specialOffset, center.y, center.z - 100f);
                _sEnd   = new Vector3(center.x + specialOffset, center.y, center.z + 100f);
            }
            else if (_params.axisMode == FeatherAxisMode.XZ)
            {
                int nX = (_views.Length + 1) / 2;
                int nZ = _views.Length / 2;
                if ((_specialIndex % 2) == 0)
                {
                    int k = _specialIndex / 2;
                    float offX = (k - (nX - 1) * 0.5f) * spacing;
                    _sStart = new Vector3(center.x - 100f, center.y, center.z + offX);
                    _sEnd   = new Vector3(center.x + 100f, center.y, center.z + offX);
                }
                else
                {
                    int k = (_specialIndex - 1) / 2;
                    float offZ = (k - (nZ - 1) * 0.5f) * spacing;
                    _sStart = new Vector3(center.x + offZ, center.y, center.z - 100f);
                    _sEnd   = new Vector3(center.x + offZ, center.y, center.z + 100f);
                }
            }
            else
            {
                _sStart = new Vector3(center.x - 100f, center.y, center.z - 100f + specialOffset);
                _sEnd   = new Vector3(center.x + 100f, center.y, center.z + 100f + specialOffset);
            }

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
            float spacing = Mathf.Max(0.1f, _params.margin);
            int n = _views.Length;

            for (int i = 0; i < n; i++)
            {
                Vector3 start, end;

                switch (_params.axisMode)
                {
                    case FeatherAxisMode.X:
                    {
                        float offset = (i - (n - 1) * 0.5f) * spacing;
                        start = new Vector3(center.x - 100f, center.y, center.z + offset);
                        end   = new Vector3(center.x + 100f, center.y, center.z + offset);
                        break;
                    }
                    case FeatherAxisMode.Z:
                    {
                        float offset = (i - (n - 1) * 0.5f) * spacing;
                        start = new Vector3(center.x + offset, center.y, center.z - 100f);
                        end   = new Vector3(center.x + offset, center.y, center.z + 100f);
                        break;
                    }
                    case FeatherAxisMode.XZ:
                    {
                        int nX = (n + 1) / 2;
                        int nZ = n / 2;
                        if ((i % 2) == 0)
                        {
                            int k = i / 2;
                            float offset = (k - (nX - 1) * 0.5f) * spacing;
                            start = new Vector3(center.x - 100f, center.y, center.z + offset);
                            end   = new Vector3(center.x + 100f, center.y, center.z + offset);
                        }
                        else
                        {
                            int k = (i - 1) / 2;
                            float offset = (k - (nZ - 1) * 0.5f) * spacing;
                            start = new Vector3(center.x + offset, center.y, center.z - 100f);
                            end   = new Vector3(center.x + offset, center.y, center.z + 100f);
                        }
                        break;
                    }
                    case FeatherAxisMode.Diagonal:
                    default:
                    {
                        float offset = (i - (n - 1) * 0.5f) * spacing;
                        start = new Vector3(center.x - 100f, center.y, center.z - 100f + offset);
                        end   = new Vector3(center.x + 100f, center.y, center.z + 100f + offset);
                        break;
                    }
                }

                Mesh mesh = _views[i].Mesh;
                Transform mT = _views[i].MeshFilter.transform;
                Vector3[] mv = mesh.vertices;
                Vector3[] verts = new Vector3[4];
                verts[0] = mT.TransformPoint(mv[0]);
                verts[1] = mT.TransformPoint(mv[1]);
                verts[2] = mT.TransformPoint(mv[2]);
                verts[3] = mT.TransformPoint(mv[3]);

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
            float spacing = Mathf.Max(0.1f, _params.margin);
            int n = _views != null ? _views.Length : 0;

            var sLr = (_views != null && _specialIndex >= 0 && _specialIndex < _views.Length) ? _views[_specialIndex].Line : null;
            if (sLr != null) sLr.enabled = false;
            var sMr = (_views != null && _specialIndex >= 0 && _specialIndex < _views.Length) ? _views[_specialIndex].MeshRenderer : null;
            if (sMr != null) sMr.enabled = false;

            yield return new WaitForSeconds(0.5f);

            EnsureAudio();
            _audio?.PlayAudio(AudioClipType.StrongWindTornado1SFX, AudioChannelType.Fx, AudioPlayType.OneShot);

            int lastIndex = effects.Count - 1;
            Vector3 playerWorld = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
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

                Vector3 start, end;
                switch (_params.axisMode)
                {
                    case FeatherAxisMode.X:
                    {
                        float offset = (i - (n - 1) * 0.5f) * spacing;
                        start = new Vector3(center.x - 100f, center.y, center.z + offset);
                        end   = new Vector3(center.x + 100f, center.y, center.z + offset);
                        break;
                    }
                    case FeatherAxisMode.Z:
                    {
                        float offset = (i - (n - 1) * 0.5f) * spacing;
                        start = new Vector3(center.x + offset, center.y, center.z - 100f);
                        end   = new Vector3(center.x + offset, center.y, center.z + 100f);
                        break;
                    }
                    case FeatherAxisMode.XZ:
                    {
                        int nX = (n + 1) / 2;
                        int nZ = n / 2;
                        if ((i % 2) == 0)
                        {
                            int k = i / 2;
                            float offset = (k - (nX - 1) * 0.5f) * spacing;
                            start = new Vector3(center.x - 100f, center.y, center.z + offset);
                            end   = new Vector3(center.x + 100f, center.y, center.z + offset);
                        }
                        else
                        {
                            int k = (i - 1) / 2;
                            float offset = (k - (nZ - 1) * 0.5f) * spacing;
                            start = new Vector3(center.x + offset, center.y, center.z - 100f);
                            end   = new Vector3(center.x + offset, center.y, center.z + 100f);
                        }
                        break;
                    }
                    case FeatherAxisMode.Diagonal:
                    default:
                    {
                        float offset = (i - (n - 1) * 0.5f) * spacing;
                        start = new Vector3(center.x - 100f, center.y, center.z - 100f + offset);
                        end   = new Vector3(center.x + 100f, center.y, center.z + 100f + offset);
                        break;
                    }
                }

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
                    if (_views[i]?.Line != null) Object.Destroy(_views[i].Line.gameObject);
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
                    var v = _views[i];
                    if (v?.Line != null) v.Line.enabled = visible;
                    if (v?.MeshRenderer != null) v.MeshRenderer.enabled = visible;
                }
            }
            if (_singleArrow != null) _singleArrow.enabled = visible;
        }
    }
}
