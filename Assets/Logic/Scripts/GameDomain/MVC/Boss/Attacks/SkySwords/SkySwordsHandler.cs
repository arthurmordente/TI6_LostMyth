using System.Collections.Generic;
using UnityEngine;
	using Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core;
using Logic.Scripts.GameDomain.MVC.Abilitys;

namespace Logic.Scripts.GameDomain.MVC.Boss.Attacks.SkySwords
{
	public sealed class SkySwordsHandler : IBossAttackHandler, Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility
	{
		private readonly float _radius;
		private readonly float _ringWidth;
		private readonly bool _isPull; // true = puxa para o centro; false = empurra para fora
		private readonly bool _telegraphDisplacementEnabled;

		private Transform _parent;
		private ArenaPosReference _arena;
			private LineRenderer _ring;
			private MeshFilter _discFilter;
			private MeshRenderer _discRenderer;
			private LineRenderer _arrow;
		private Vector3 _centerWorld;
			private Logic.Scripts.Services.UpdateService.IUpdateSubscriptionService _updateSvc;
			private float _yOffset = 0.05f;
			private int _rqAdd = 0;

			// Exposição global para efeitos (Grapple/Knockback) direcionarem pelo centro do SkySwords
			public static bool CurrentDisplacementEnabled;
			public static Vector3 CurrentCenterWorld;

		// Prime para decidir push/pull automaticamente a partir da lista de efeitos do ataque
		private static bool? _nextIsPull = null;
		private bool? _pullOverride = null;
		public static void PrimeNextTelegraphPull(bool isPull) => _nextIsPull = isPull;

		private readonly Material _lineMaterial;
		private readonly Material _meshMaterial;

		public SkySwordsHandler(float radius, float ringWidth, bool isPull, bool telegraphDisplacementEnabled, Material lineMaterial, Material meshMaterial)
		{
			_radius = Mathf.Max(0.1f, radius);
			_ringWidth = Mathf.Max(0.02f, ringWidth);
			_isPull = isPull;
			_telegraphDisplacementEnabled = telegraphDisplacementEnabled;
			_lineMaterial = lineMaterial;
			_meshMaterial = meshMaterial;
		}

		public void PrepareTelegraph(Transform parentTransform)
		{
			_parent = parentTransform;
			_arena = Object.FindFirstObjectByType<ArenaPosReference>(FindObjectsInactive.Exclude);
			_centerWorld = ResolvePlayerWorldPosition();
				_updateSvc = TryFindUpdateServiceInScene();

				// Aplica override de pull/push se houver prime configurado para este telegraph
				if (_nextIsPull.HasValue)
				{
					_pullOverride = _nextIsPull.Value;
					_nextIsPull = null;
				}

			var go = new GameObject("SkySwords_Ring");
			go.transform.SetParent(parentTransform, false);
			_ring = go.AddComponent<LineRenderer>();
			var layering = Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphLayeringLocator.Service;
			var layer = layering != null ? layering.Register(preferTop: _telegraphDisplacementEnabled) : default;
			_yOffset = layer.Y;
			_rqAdd = layer.QueueAdd;
			var ringMat = _lineMaterial != null ? new Material(_lineMaterial) : new Material(Shader.Find("Sprites/Default"));
			ringMat.renderQueue += _rqAdd;
			_ring.material = ringMat;
			_ring.useWorldSpace = true;
			_ring.loop = true;
			_ring.widthMultiplier = _ringWidth;
			// Cor: amarelo para ataques normais; vermelho para ataques com deslocamento
			_ring.startColor = _telegraphDisplacementEnabled ? new Color(1f, 0f, 0f, 0.8f) : new Color(1f, 1f, 0f, 0.8f);
			_ring.endColor = _ring.startColor;

			DrawCircle(_centerWorld, _radius, 64);

				// Disc fill
				var discGo = new GameObject("SkySwords_Fill");
				discGo.transform.SetParent(parentTransform, false);
				_discFilter = discGo.AddComponent<MeshFilter>();
				_discRenderer = discGo.AddComponent<MeshRenderer>();
				var discMat = _meshMaterial != null ? new Material(_meshMaterial) : new Material(Shader.Find("Sprites/Default"));
				discMat.renderQueue += _rqAdd;
				_discRenderer.material = discMat;
				float effectiveRadius = Mathf.Max(0.01f, _radius - _ringWidth * 0.5f);
				_discFilter.sharedMesh = BuildFilledDisc(effectiveRadius, 64);
				// Posiciona o fill no centro do círculo, em y offset (plano)
				_discFilter.transform.position = new Vector3(_centerWorld.x, _yOffset, _centerWorld.z);

				// Arrow (player direction), similar ao feather quando deslocamento habilitado
				if (_telegraphDisplacementEnabled)
				{
					var arrowGo = new GameObject("SkySwords_DirectionArrow");
					arrowGo.transform.SetParent(parentTransform, false);
					_arrow = arrowGo.AddComponent<LineRenderer>();
					var shader = Shader.Find("Universal Render Pipeline/Unlit");
					if (shader == null) shader = Shader.Find("Unlit/Color");
					if (shader == null) shader = Shader.Find("Sprites/Default");
					var arrowMat = new Material(shader);
					// não forçar renderQueue; manter no padrão do shader (opaco)
					// cor da seta igual à cor do material de MESH (provider já resolveu Grapple/Knockback)
					if (_meshMaterial != null)
					{
						Color col;
						if (_meshMaterial.HasProperty("_BaseColor")) col = _meshMaterial.GetColor("_BaseColor");
						else if (_meshMaterial.HasProperty("_Color")) col = _meshMaterial.color;
						else col = Color.white;
						col.a = 1f; // força opacidade total na seta
						if (arrowMat.HasProperty("_BaseColor")) arrowMat.SetColor("_BaseColor", col);
						if (arrowMat.HasProperty("_Color")) arrowMat.color = col;
					}
					_arrow.material = arrowMat;
					_arrow.useWorldSpace = true;
					_arrow.loop = false;
					_arrow.widthMultiplier = 0.08f;
					_arrow.enabled = true;
					UpdateArrow();
					_arrowUpdater = new UpdatableArrow(this);
					_updateSvc?.RegisterUpdatable(_arrowUpdater);
				}

				// Publicar centro para efeitos de deslocamento
				if (_telegraphDisplacementEnabled)
				{
					CurrentDisplacementEnabled = true;
					CurrentCenterWorld = _centerWorld;
				}

			// Start hidden; boss controller will reveal at mid prep
			SetTelegraphVisible(false);
			Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.Register(this);
		}

		public bool ComputeHits(ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
		{
			// Não fazemos pré-checagem de hit aqui; a execução aplicará efeitos condicionalmente
			return true;
		}

		public System.Collections.IEnumerator ExecuteEffects(List<AbilityEffect> effects, ArenaPosReference arenaReference, Transform originTransform, IEffectable caster)
		{
			if (effects == null || effects.Count == 0) yield break;
			if (arenaReference == null) yield break;

			IEffectable target = arenaReference.NaraController as IEffectable;
			if (target == null) yield break;

			Vector3 playerWorld = arenaReference.RelativeArenaPositionToRealPosition(arenaReference.GetPlayerArenaPosition());
			float dist = Vector3.Distance(new Vector3(playerWorld.x, _centerWorld.y, playerWorld.z), _centerWorld);

				// Ajuste de deslocamento radial (aplica independentemente do acerto de dano)
				for (int i = 0; i < effects.Count; i++)
				{
					if (effects[i] is Assets.Logic.Scripts.GameDomain.Effects.DisplacementEffect disp)
					{
						// Direção sempre do centro -> player; push empurra para fora, pull puxa para dentro
						Vector3 dir = (playerWorld - _centerWorld);
						dir.y = 0f;
						if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
						disp.direction = dir;
						disp.isPush = !UsePull();
					}
				}

				// Executa deslocamento (Grapple/Knockback/Displacement) sempre; demais efeitos apenas se dentro da área
				bool inside = dist <= _radius + 1e-4f;
				for (int i = 0; i < effects.Count; i++)
				{
					var fx = effects[i];
					if (fx == null) continue;
					bool isMove = fx is Assets.Logic.Scripts.GameDomain.Effects.DisplacementEffect
						|| fx is Logic.Scripts.GameDomain.Effects.KnockbackEffect
						|| fx is Logic.Scripts.GameDomain.Effects.GrappleEffect;
					if (!isMove && !inside) continue; // dano/aplicações só se acertar

					if (fx is IAsyncEffect asyncFx)
						yield return asyncFx.ExecuteRoutine(caster, target);
					else
						fx.Execute(caster, target);
				}
		}

		public void Cleanup()
		{
			if (_ring != null)
			{
				Object.Destroy(_ring.gameObject);
				_ring = null;
			}
				if (_discFilter != null)
				{
					Object.Destroy(_discFilter.gameObject);
					_discFilter = null;
					_discRenderer = null;
				}
				if (_arrow != null)
				{
					Object.Destroy(_arrow.gameObject);
					_arrow = null;
				}
				if (_arrowUpdater != null && _updateSvc != null)
				{
					_updateSvc.UnregisterUpdatable(_arrowUpdater);
					_arrowUpdater = null;
				}
				CurrentDisplacementEnabled = false;
				Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.Unregister(this);
		}

		public void SetTelegraphVisible(bool visible)
		{
			if (_ring != null) _ring.enabled = visible;
			if (_discRenderer != null) _discRenderer.enabled = visible;
			if (_arrow != null) _arrow.enabled = visible && _telegraphDisplacementEnabled;
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
				var mesh = new Mesh { name = "SkySwordsDiscMesh" };
				var verts = new Vector3[segments + 1];
				var tris = new int[segments * 3];
				// Centro na origem local (0,0,0) para posicionar via transform
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
					// Inverte a ordem para garantir normal para cima (Y+)
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

		private Vector3 ResolvePlayerWorldPosition()
		{
			if (_arena != null)
				return _arena.RelativeArenaPositionToRealPosition(_arena.GetPlayerArenaPosition());
			var naraView = Object.FindFirstObjectByType<Nara.NaraView>(FindObjectsInactive.Exclude);
			return naraView != null ? naraView.transform.position : Vector3.zero;
		}

			private Logic.Scripts.Services.UpdateService.IUpdateSubscriptionService TryFindUpdateServiceInScene()
			{
				try
				{
					var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
					for (int i = 0; i < all.Length; i++)
					{
						if (all[i].TryGetComponent<Logic.Scripts.Services.UpdateService.IUpdateSubscriptionService>(out var svc))
							return svc;
					}
				}
				catch { }
				return null;
			}

			private void UpdateArrow()
			{
				if (_arrow == null) return;
				Vector3 playerWorld = ResolvePlayerWorldPosition();
				Vector3 dir = (playerWorld - _centerWorld);
				dir.y = 0f;
				if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;
				dir.Normalize();
				dir = UsePull() ? -dir : dir;

				// Seta sempre acima dos telegraphs
				float y = Mathf.Max(_yOffset + 0.25f, 0.3f);
				float outOffset = Mathf.Max(0.35f, _radius * 0.1f);
				float shaftLen = Mathf.Max(0.75f, _radius * 0.2f);
				float headLen = shaftLen * 0.35f;
				float headHalfW = headLen * 0.6f;

				Vector3 origin = new Vector3(playerWorld.x, y, playerWorld.z) + dir * outOffset;
				Vector3 tip = origin + dir * shaftLen;
				Vector3 tail = origin - dir * 0.25f;

				Vector3 side = new Vector3(-dir.z, 0f, dir.x);
				Vector3 leftWing = tip - dir * headLen + side * headHalfW;
				Vector3 rightWing = tip - dir * headLen - side * headHalfW;

				_arrow.enabled = true;
				_arrow.positionCount = 5;
				_arrow.SetPosition(0, tail);
				_arrow.SetPosition(1, tip);
				_arrow.SetPosition(2, leftWing);
				_arrow.SetPosition(3, tip);
				_arrow.SetPosition(4, rightWing);
			}

			private sealed class UpdatableArrow : Logic.Scripts.Services.UpdateService.IUpdatable
			{
				private readonly SkySwordsHandler _owner;
				public UpdatableArrow(SkySwordsHandler owner) { _owner = owner; }
				public void ManagedUpdate()
				{
					_owner?.UpdateArrow();
				}
			}

			private UpdatableArrow _arrowUpdater;

			private bool UsePull()
			{
				return _pullOverride.HasValue ? _pullOverride.Value : _isPull;
			}
	}
}

