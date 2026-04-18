using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback
{
    /// <summary>
    /// Skill targeting highlight: appends one runtime clone of <see cref="_highlightMaterialTemplate"/> to each
    /// mesh renderer's <see cref="Renderer.sharedMaterials"/> while aiming (Unity draws extra materials on the
    /// same submesh as an additional pass). Restores the previous array and destroys the clone when highlight ends.
    /// </summary>
    public sealed class SkillTargetingHighlightPresenter : MonoBehaviour
    {
        [Tooltip("Art material to append while aiming (one clone per renderer; the asset is never modified).")]
        [SerializeField] private Material _highlightMaterialTemplate;

        [Tooltip("If null, uses this transform. Set to the imported model root if needed.")]
        [SerializeField] private Transform _modelRoot;

        [SerializeField] private bool _overrideMainColor;
        [SerializeField] private Color _mainColor = new Color(8f, 0f, 0f, 1f);

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private Material[][] _backupSharedMaterials;
        private readonly List<Material> _runtimeHighlightInstances = new List<Material>();
        private bool _highlighted;

        public void SetHighlighted(bool active)
        {
            if (active == _highlighted) return;

            if (active)
            {
                if (_highlightMaterialTemplate == null)
                    return;
                if (!TryEnterHighlight())
                    return;
            }
            else
                ExitHighlight();

            _highlighted = active;
        }

        private bool TryEnterHighlight()
        {
            CollectRenderers();
            if (_renderers.Count == 0)
                return false;

            ClearHighlightInstances();
            if (_backupSharedMaterials == null || _backupSharedMaterials.Length != _renderers.Count)
                _backupSharedMaterials = new Material[_renderers.Count][];

            for (int i = 0; i < _renderers.Count; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;

                Material[] orig = r.sharedMaterials;
                _backupSharedMaterials[i] = orig != null && orig.Length > 0
                    ? (Material[])orig.Clone()
                    : Array.Empty<Material>();

                Material highlightInstance = new Material(_highlightMaterialTemplate);
                if (_overrideMainColor && highlightInstance.HasProperty("_MainColor"))
                    highlightInstance.SetColor("_MainColor", _mainColor);

                _runtimeHighlightInstances.Add(highlightInstance);

                int n = _backupSharedMaterials[i].Length;
                var combined = new Material[n + 1];
                if (n > 0)
                    Array.Copy(_backupSharedMaterials[i], combined, n);
                combined[n] = highlightInstance;

                r.sharedMaterials = combined;
            }

            return true;
        }

        private void ExitHighlight()
        {
            RestoreBackedUpMaterials();
            ClearHighlightInstances();
        }

        private void RestoreBackedUpMaterials()
        {
            if (_backupSharedMaterials == null || _renderers.Count == 0) return;

            for (int i = 0; i < _renderers.Count; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;

                Material[] bak = i < _backupSharedMaterials.Length ? _backupSharedMaterials[i] : null;
                r.sharedMaterials = bak != null && bak.Length > 0 ? bak : Array.Empty<Material>();
                r.SetPropertyBlock(null);
            }
        }

        private void ClearHighlightInstances()
        {
            for (int i = 0; i < _runtimeHighlightInstances.Count; i++)
            {
                Material m = _runtimeHighlightInstances[i];
                if (m != null)
                    Destroy(m);
            }
            _runtimeHighlightInstances.Clear();
        }

        private void CollectRenderers()
        {
            _renderers.Clear();
            Transform root = _modelRoot != null ? _modelRoot : transform;

            foreach (SkinnedMeshRenderer r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r != null) _renderers.Add(r);
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
                if (r != null) _renderers.Add(r);
        }

        private void OnDestroy()
        {
            if (_highlighted)
            {
                _highlighted = false;
                RestoreBackedUpMaterials();
            }
            ClearHighlightInstances();
        }
    }
}
