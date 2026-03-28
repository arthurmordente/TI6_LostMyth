using System.Collections;
using UnityEngine;

namespace Assets.Logic.Scripts.GameDomain.Effects
{
	/// <summary>
	/// Quick damage feedback (model): blink by swapping renderers' materials to a red albedo.
	/// Designed so later you can replace the swap logic with a shader-based flash animation.
	/// </summary>
	public sealed class DamageFlashPresenter : MonoBehaviour
	{
		[SerializeField] private float _flashSeconds = 0.10f;
		[SerializeField] private Color _flashColor = new Color(1f, 0.15f, 0.15f, 1f);

		private Renderer[] _renderers;
		private Material[][] _originalSharedMaterials;
		private Material _flashMaterial;
		private Coroutine _running;

		public void TriggerFlash()
		{
			if (!isActiveAndEnabled) return;
			EnsureCache();

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

		private void EnsureCache()
		{
			if (_renderers != null) return;
			_renderers = GetComponentsInChildren<Renderer>(true);
			_originalSharedMaterials = new Material[_renderers.Length][];

			for (int i = 0; i < _renderers.Length; i++)
			{
				var r = _renderers[i];
				_originalSharedMaterials[i] = (r != null) ? r.sharedMaterials : null;
			}
		}

		private void ApplyFlash()
		{
			if (_renderers == null) return;
			EnsureFlashMaterial();
			if (_flashMaterial == null) return;

			for (int i = 0; i < _renderers.Length; i++)
			{
				var r = _renderers[i];
				if (r == null) continue;

				var originals = _originalSharedMaterials != null && i < _originalSharedMaterials.Length
					? _originalSharedMaterials[i]
					: null;
				if (originals == null || originals.Length == 0) continue;

				var flashArr = new Material[originals.Length];
				for (int m = 0; m < flashArr.Length; m++) flashArr[m] = _flashMaterial;
				r.sharedMaterials = flashArr;
			}
		}

		private void Restore()
		{
			if (_renderers == null || _originalSharedMaterials == null) return;

			for (int i = 0; i < _renderers.Length; i++)
			{
				var r = _renderers[i];
				if (r == null) continue;

				if (i < _originalSharedMaterials.Length && _originalSharedMaterials[i] != null)
					r.sharedMaterials = _originalSharedMaterials[i];
			}
		}

		private void EnsureFlashMaterial()
		{
			if (_flashMaterial != null) return;

			Shader lit = Shader.Find("Universal Render Pipeline/Lit");
			if (lit == null) lit = Shader.Find("Standard");
			if (lit == null) return;

			_flashMaterial = new Material(lit);

			// Common properties across common shaders
			if (_flashMaterial.HasProperty("_BaseColor")) _flashMaterial.SetColor("_BaseColor", _flashColor);
			if (_flashMaterial.HasProperty("_Color")) _flashMaterial.SetColor("_Color", _flashColor);
			if (_flashMaterial.HasProperty("_EmissionColor")) _flashMaterial.SetColor("_EmissionColor", _flashColor);
		}

		private void OnDestroy()
		{
			if (_flashMaterial != null) Destroy(_flashMaterial);
		}
	}
}

