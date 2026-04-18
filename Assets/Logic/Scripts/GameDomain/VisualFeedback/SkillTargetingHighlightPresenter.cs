using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback
{
    /// <summary>
    /// Skill targeting highlight: temporarily replaces each material slot with a runtime clone of
    /// <see cref="_highlightMaterialTemplate"/> (e.g. team fresnel material). Restores
    /// <see cref="Renderer.sharedMaterials"/> when highlight ends.
    /// </summary>
    public sealed class SkillTargetingHighlightPresenter : MonoBehaviour
    {
        [Tooltip("Art material to show while aiming (cloned per slot; the asset is never modified).")]
        [SerializeField] private Material _highlightMaterialTemplate;

        [Tooltip("If null, uses this transform. Set to the imported model root if needed.")]
        [SerializeField] private Transform _modelRoot;

        [SerializeField] private bool _overrideMainColor;
        [SerializeField] private Color _mainColor = new Color(8f, 0f, 0f, 1f);

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private Material[][] _backupSharedMaterials;
        private readonly List<Material> _runtimeMaterialsToDestroy = new List<Material>();
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

            DisposeRuntimeMaterials();
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

                if (orig == null || orig.Length == 0)
                    continue;

                var replacement = new Material[orig.Length];
                for (int m = 0; m < orig.Length; m++)
                {
                    Material instance = new Material(_highlightMaterialTemplate);
                    if (_overrideMainColor && instance.HasProperty("_MainColor"))
                        instance.SetColor("_MainColor", _mainColor);

                    _runtimeMaterialsToDestroy.Add(instance);
                    replacement[m] = instance;
                }

                r.sharedMaterials = replacement;
            }

            return true;
        }

        private void ExitHighlight()
        {
            DisposeRuntimeMaterials();

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

        private void DisposeRuntimeMaterials()
        {
            for (int i = 0; i < _runtimeMaterialsToDestroy.Count; i++)
            {
                Material m = _runtimeMaterialsToDestroy[i];
                if (m != null)
                    Destroy(m);
            }
            _runtimeMaterialsToDestroy.Clear();
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
            DisposeRuntimeMaterials();
        }
    }
}
