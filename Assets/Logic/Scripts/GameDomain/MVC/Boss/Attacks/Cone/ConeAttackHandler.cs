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
        private readonly float _telegraphUniformScale;
		private class ConeSubView
        {
            public GameObject Root;
        }
		private ConeSubView[] _views;
		private readonly Material _lineMaterial;
		private readonly Material _meshMaterial;
		private readonly GameObject _telegraphPrefab;
		private Logic.Scripts.GameDomain.MVC.Boss.Telegraph.ITelegraphLayeringService.TelegraphLayer _layer;

		public ConeAttackHandler(float radius, float angleDeg, int sides, float[] yaws, Material lineMaterial, Material meshMaterial, GameObject telegraphPrefab = null, float telegraphUniformScale = 1f)
        {
            _radius = radius;
            _angleDeg = angleDeg;
            _sides = sides;
            _yaws = yaws;
            _telegraphUniformScale = Mathf.Max(0.001f, telegraphUniformScale);
			_lineMaterial = lineMaterial;
			_meshMaterial = meshMaterial;
			_telegraphPrefab = telegraphPrefab;
        }

        public void PrepareTelegraph(Transform parentTransform)
        {
			var layering = Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphLayeringLocator.Service;
			_layer = layering != null ? layering.Register(preferTop: false) : default;

            if (_yaws == null || _yaws.Length == 0) return;
            _views = new ConeSubView[_yaws.Length];
            for (int i = 0; i < _yaws.Length; i++)
            {
                ConeSubView v = new ConeSubView();

                Vector3 origin = parentTransform.position;
                Vector3 parentFwd = new Vector3(parentTransform.forward.x, 0f, parentTransform.forward.z);
                if (parentFwd.sqrMagnitude < 1e-6f) parentFwd = Vector3.forward;
                Vector3 forward = Quaternion.Euler(0f, _yaws[i], 0f) * parentFwd;
				Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
				if (_telegraphPrefab != null)
				{
					v.Root = Object.Instantiate(_telegraphPrefab, new Vector3(origin.x, _layer.Y, origin.z), rotation, parentTransform);
					float scale = _telegraphUniformScale;
					v.Root.transform.localScale = new Vector3(scale, 1f, scale);
				}
				else
				{
					v.Root = new GameObject("ConeTelegraphPlaceholder");
					v.Root.transform.SetParent(parentTransform, false);
					v.Root.transform.SetPositionAndRotation(new Vector3(origin.x, _layer.Y, origin.z), rotation);
				}

                _views[i] = v;
            }

            // Start hidden; boss controller will reveal at mid prep
            SetTelegraphVisible(false);
            Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.Register(this);
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
                    Object.Destroy(_views[i].Root);
            }
            _views = null;

			var layering = Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphLayeringLocator.Service;
			if (layering != null && _layer.Id >= 0) layering.Unregister(_layer.Id);
            Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.Unregister(this);
        }

        public void SetTelegraphVisible(bool visible)
        {
            if (_views == null) return;
            for (int i = 0; i < _views.Length; i++)
            {
                var v = _views[i];
                if (v?.Root != null) v.Root.SetActive(visible);
            }
        }
    }
}


