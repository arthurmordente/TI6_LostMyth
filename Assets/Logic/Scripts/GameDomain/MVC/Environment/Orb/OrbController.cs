using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Environment.Orb;
using Logic.Scripts.Turns;

namespace Logic.Scripts.GameDomain.MVC.Environment.Orb
{
    public class OrbController : MonoBehaviour, IEffectable, IEnvironmentTurnActor
    {
        public static readonly System.Collections.Generic.List<OrbController> Instances = new System.Collections.Generic.List<OrbController>();
			// Current transform to follow. Defaults to player; can be reassigned (e.g., to a clone).
			private Transform _followTarget;
        private ArenaPosReference _arena;
        private INaraController _nara;
        private OrbView _view;
        private OrbRegistry _registry;

        private int _hp;
        private float _radius;
        private float _maxRadius;
        private float _initialRadius;
        private float _growStep;
        private float _moveStep;
        private int _baseDamage;
        private int _damageExponent;
        private int _stunMoveTurns;
        private int _stunAttackTurns;
        private bool _isMoving;
        public System.Threading.Tasks.Task CurrentTickTask { get; private set; }

        // Remove automaticamente do registro genérico do Environment quando a orb estiver destruída
        public bool RemoveAfterRun => _hp <= 0;

        [SerializeReference] private List<AbilityEffect> _effects;

        public void Initialize(ArenaPosReference arena, OrbRegistry registry, float moveStep, float growStep, float initialRadius, float maxRadius, int baseDamage, int initialHp)
        {
            _arena = arena;
            _nara = arena != null ? arena.NaraController : null;
            _registry = registry;
            _moveStep = moveStep;
            _initialRadius = initialRadius;
            _radius = initialRadius;
            _maxRadius = maxRadius;
            _baseDamage = baseDamage;
            _hp = initialHp;
            _damageExponent = 0;
            // Reach max in 8 turns
            _growStep = (_maxRadius > _initialRadius) ? (_maxRadius - _initialRadius) / 8f : 0f;
            _view = GetComponent<OrbView>();
            if (_view != null)
            {
                _view.PrepareTelegraph();
                _view.UpdateRadius(_radius);
            }
			// Default: follow player. This can be changed at runtime via SetFollowTarget/RetargetAllTo.
			_followTarget = (_nara != null && _nara.NaraViewGO != null) ? _nara.NaraViewGO.transform : null;
            UnityEngine.Debug.Log($"[Orb] Initialized at {transform.position} radius={_radius} max={_maxRadius}");
        }

        private void OnEnable()
        {
            if (!Instances.Contains(this)) Instances.Add(this);
        }

        public void TickTurn() { StartTickAsync(); }

        public void StartTickAsync()
        {
            CurrentTickTask = DoTickAsync();
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_hp <= 0) return;
            StartTickAsync();
            var t = CurrentTickTask;
            if (t != null) await t;
        }

        private async System.Threading.Tasks.Task DoTickAsync()
        {
            if (_hp <= 0) return;
            UnityEngine.Debug.Log($"[Environment][Orb] Tick -> pos={transform.position} radius={_radius:0.##} exp={_damageExponent}");
            await System.Threading.Tasks.Task.Delay(500);
            if (_stunAttackTurns > 0) { _stunAttackTurns--; } else { PerformAction(); }
            await AnimateGrowAsync(0.5f);
            if (_stunMoveTurns > 0) { _stunMoveTurns--; }
            else { await MoveAsync(0.5f); }
            await System.Threading.Tasks.Task.Delay(500);
        }

        public void DoAction()
        {
            if (_hp <= 0) return;
            if (_stunAttackTurns > 0) { _stunAttackTurns--; return; }
            PerformAction();
        }

		public async System.Threading.Tasks.Task MoveAsync(float duration)
        {
			if (_hp <= 0) return;
			if (_stunMoveTurns > 0) { _stunMoveTurns--; return; }
			if (_isMoving) return;
			// Ensure a valid follow target; fallback to player if available.
			if (_followTarget == null && _nara != null && _nara.NaraViewGO != null) _followTarget = _nara.NaraViewGO.transform;
			Transform target = _followTarget;
			if (target == null) return;
            Vector3 from = transform.position;
            Vector3 to = target.position;
            Vector3 delta = to - from; delta.y = 0f;
            float dist = delta.magnitude;
			if (dist < 1e-3f) return;
            float step = Mathf.Min(_moveStep, dist);
            Vector3 end = from + (delta / dist) * step; end.y = from.y;
			bool willReach = step >= dist - 1e-4f;
			await SmoothMoveTo(end, duration);
			// If we reached the current follow target, detonate and apply AoE effects.
			if (willReach && _hp > 0) { Explode(); }
        }

        private System.Threading.Tasks.Task SmoothMoveTo(Vector3 end, float duration)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            StartCoroutine(DoSmoothMove(end, duration, tcs));
            return tcs.Task;
        }

        private System.Collections.IEnumerator DoSmoothMove(Vector3 end, float duration, System.Threading.Tasks.TaskCompletionSource<bool> tcs)
        {
            _isMoving = true;
            Vector3 start = transform.position;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float v = Mathf.Clamp01(timer / duration);
                Vector3 p = Vector3.Lerp(start, end, v);
                p.y = start.y;
                transform.position = p;
                if (_view != null) _view.UpdateRadius(_radius);
                yield return null;
            }
            transform.position = end;
            if (_view != null) _view.UpdateRadius(_radius);
            _isMoving = false;
            tcs.TrySetResult(true);
        }

		private void PerformAction()
        {
			// Per-turn orb damage removed: orb only damages when it explodes upon reaching its follow target.
			_damageExponent = Mathf.Min(_damageExponent + 1, 30);
        }

		// Explodes at current position, applying configured effects to all IEffectable within the current radius.
		// Note: Being destroyed (HP <= 0) does not cause this effect; only calling Explode does.
		private void Explode()
		{
			try
			{
				if (_effects != null && _effects.Count > 0)
				{
					var hits = Physics.OverlapSphere(transform.position, _radius);
					if (hits != null && hits.Length > 0)
					{
						var caster = this as IEffectable;
						var affected = new System.Collections.Generic.HashSet<IEffectable>();
						for (int i = 0; i < hits.Length; i++)
						{
							var h = hits[i];
							if (h == null) continue;
							var eff = h.GetComponentInParent<IEffectable>();
							if (eff == null) continue;
							if (!affected.Add(eff)) continue;
							for (int e = 0; e < _effects.Count; e++)
							{
								var fx = _effects[e];
								fx?.Execute(caster, eff);
							}
						}
					}
				}
			}
			finally
			{
				// Mark as dead and remove from scene; OnDestroy only unregisters.
				_hp = 0;
				Destroy(gameObject);
			}
		}

		// Public API to change the orb's follow target (e.g., switch to a placed clone).
		public void SetFollowTarget(Transform t)
		{
			_followTarget = t;
		}

		// Convenience API to retarget all active orbs at once.
		public static void RetargetAllTo(Transform t)
		{
			if (t == null) return;
			for (int i = 0; i < Instances.Count; i++)
			{
				var orb = Instances[i];
				if (orb != null) orb.SetFollowTarget(t);
			}
		}
        private System.Threading.Tasks.Task AnimateGrowAsync(float duration)
        {
            if (_radius >= _maxRadius) return System.Threading.Tasks.Task.CompletedTask;
            float start = _radius;
            float target = Mathf.Min(_maxRadius, _radius + _growStep);
            return SmoothGrow(start, target, duration);
        }

        private System.Threading.Tasks.Task SmoothGrow(float start, float target, float duration)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            StartCoroutine(DoSmoothGrow(start, target, duration, tcs));
            return tcs.Task;
        }

        private System.Collections.IEnumerator DoSmoothGrow(float start, float target, float duration, System.Threading.Tasks.TaskCompletionSource<bool> tcs)
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float v = Mathf.Clamp01(timer / duration);
                _radius = Mathf.Lerp(start, target, v);
                if (_view != null) _view.UpdateRadius(_radius);
                yield return null;
            }
            _radius = target;
            if (_view != null) _view.UpdateRadius(_radius);
            tcs.TrySetResult(true);
        }

        private void OnDestroy()
        {
            Instances.Remove(this);
            _registry?.Unregister(this);
            // Remover do registro genérico publicado globalmente
            if (Logic.Scripts.Turns.EnvironmentActorsRegistryService.Instance != null)
            {
                Logic.Scripts.Turns.EnvironmentActorsRegistryService.Instance.Remove(this);
            }
        }

        public void TakeDamage(int amount)
        {
            _hp -= Mathf.Max(0, amount);
            Debug.Log(_hp);
            if (_hp <= 0)
            {
                Destroy(gameObject);
            }
        }

        public void Heal(int amount) {}
        public void TakeDamagePerTurn(int damageAmount, int duration) {}
        public void HealPerTurn(int healAmount, int duration) {}

        public Transform GetReferenceTransform() { return transform; }
        public void PreviewHeal(int healAmound) {}
        public void PreviewDamage(int damageAmound) {}
        public void ResetPreview() {}

        public Transform GetTransformCastPoint() {
            return transform;
        }

        public LineRenderer GetPointLineRenderer() => null;

        public GameObject GetReferenceTargetPrefab() {
            return gameObject;
        }
    }
}


