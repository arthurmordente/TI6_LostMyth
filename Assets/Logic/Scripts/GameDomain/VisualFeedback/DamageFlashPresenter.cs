using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback
{
    /// <summary>
    /// Damage feedback: briefly flashes all mesh renderers under _modelRoot.
    /// Assign _modelRoot to the root of the imported Blender model in the Inspector.
    /// When left empty, falls back to this GameObject itself.
    /// </summary>
    public sealed class DamageFlashPresenter : MonoBehaviour
    {
        [SerializeField] private float _flashSeconds = 0.10f;
        [SerializeField] private Color _flashColor = Color.red;

        [Tooltip("Root of the model whose renderers should flash. All SkinnedMesh/MeshRenderers under it will be affected.")]
        [SerializeField] private Transform _modelRoot;

        private Renderer[] _targetRenderers;
        private Material[][] _originalMaterials;
        private Material _flashMaterial;
        private Color _builtColor;
        private Coroutine _running;

        public void TriggerFlash()
        {
            if (!isActiveAndEnabled) return;
            EnsureSetup();
            if (_flashMaterial == null || _targetRenderers == null || _targetRenderers.Length == 0) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            ApplyFlash();
            yield return new WaitForSeconds(Mathf.Max(0.01f, _flashSeconds));
            Restore();
            _running = null;
        }

        private void EnsureSetup()
        {
            Transform root = _modelRoot != null ? _modelRoot : transform;

            if (_targetRenderers == null || _targetRenderers.Length == 0)
            {
                var list = new List<Renderer>();
                foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) list.Add(r);
                foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true)) list.Add(r);
                _targetRenderers = list.ToArray();

                _originalMaterials = new Material[_targetRenderers.Length][];
                for (int i = 0; i < _targetRenderers.Length; i++)
                    _originalMaterials[i] = _targetRenderers[i] != null ? _targetRenderers[i].materials : null;
            }

            if (_flashMaterial == null || _builtColor != _flashColor)
            {
                if (_flashMaterial != null) Destroy(_flashMaterial);

                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) return;

                _flashMaterial = new Material(shader);
                if (_flashMaterial.HasProperty("_BaseColor")) _flashMaterial.SetColor("_BaseColor", _flashColor);
                if (_flashMaterial.HasProperty("_Color"))     _flashMaterial.SetColor("_Color",      _flashColor);
                _builtColor = _flashColor;
            }
        }

        private void ApplyFlash()
        {
            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                var r = _targetRenderers[i];
                if (r == null) continue;
                var originals = _originalMaterials != null && i < _originalMaterials.Length ? _originalMaterials[i] : null;
                if (originals == null || originals.Length == 0) continue;
                var flashSet = new Material[originals.Length];
                for (int m = 0; m < flashSet.Length; m++) flashSet[m] = _flashMaterial;
                r.materials = flashSet;
            }
        }

        private void Restore()
        {
            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                var r = _targetRenderers[i];
                if (r == null) continue;
                if (_originalMaterials != null && i < _originalMaterials.Length && _originalMaterials[i] != null)
                    r.materials = _originalMaterials[i];
            }
        }

        private void OnDestroy()
        {
            if (_flashMaterial != null) Destroy(_flashMaterial);
        }
    }
}
