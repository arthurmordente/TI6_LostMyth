using System.Collections.Generic;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Abilitys;

namespace Logic.Scripts.GameDomain.MVC.Boss.Telegraph
{
	public class TelegraphMaterialProvider : ITelegraphMaterialProvider
	{
		private readonly TelegraphMaterialConfig _config;

		public TelegraphMaterialProvider(TelegraphMaterialConfig config)
		{
			_config = config;
		}

		public Material GetMaterial(bool telegraphDisplacementEnabled, IList<AbilityEffect> effects)
		{
			// Backwards compat: treat as mesh (fills/discs)
			return GetMeshMaterial(telegraphDisplacementEnabled, effects);
		}

		public Material GetLineMaterial(bool telegraphDisplacementEnabled, IList<AbilityEffect> effects)
		{
			UnityEngine.Debug.Log($"[TelegraphProvider] GetLineMaterial called. disp={telegraphDisplacementEnabled} cfg={( _config != null ? _config.name : "NULL")}");
			if (_config == null)
			{
				UnityEngine.Debug.LogWarning("[TelegraphProvider] Config is NULL. Falling back to Sprites/Default.");
				return new Material(Shader.Find("Sprites/Default"));
			}

			// Determine effect class (Normal/Grapple/Knockback)
			bool hasGrapple = false;
			bool hasKnock = false;
			if (effects != null)
			{
				for (int i = 0; i < effects.Count; i++)
				{
					var fx = effects[i];
					if (fx == null) continue;
					if (fx is Logic.Scripts.GameDomain.Effects.GrappleEffect) hasGrapple = true;
					else if (fx is Logic.Scripts.GameDomain.Effects.KnockbackEffect) hasKnock = true;
				}
			}

			Material chosen = null;
			if (telegraphDisplacementEnabled)
			{
				if (hasGrapple) chosen = _config.GrappleLineMaterial ?? _config.GrappleAreaMaterial;
				else if (hasKnock) chosen = _config.KnockbackLineMaterial ?? _config.KnockbackAreaMaterial;
			}

			if (chosen == null)
				chosen = _config.NormalLineMaterial ?? _config.NormalAreaMaterial;

			if (chosen == null) chosen = new Material(Shader.Find("Sprites/Default"));
			UnityEngine.Debug.Log($"[TelegraphProvider] Using LINE material: {chosen.name}");
			return chosen;
		}

		public Material GetMeshMaterial(bool telegraphDisplacementEnabled, IList<AbilityEffect> effects)
		{
			UnityEngine.Debug.Log($"[TelegraphProvider] GetMeshMaterial called. disp={telegraphDisplacementEnabled} cfg={( _config != null ? _config.name : "NULL")}");
			if (_config == null)
			{
				UnityEngine.Debug.LogWarning("[TelegraphProvider] Config is NULL. Falling back to Sprites/Default.");
				return new Material(Shader.Find("Sprites/Default"));
			}

			if (!telegraphDisplacementEnabled)
			{
				var mat = _config.NormalAreaMaterial != null ? _config.NormalAreaMaterial : new Material(Shader.Find("Sprites/Default"));
				UnityEngine.Debug.Log($"[TelegraphProvider] Using Normal MESH material: {(mat != null ? mat.name : "NULL")}");
				return mat;
			}

			bool hasGrapple = false;
			bool hasKnock = false;
			if (effects != null)
			{
				for (int i = 0; i < effects.Count; i++)
				{
					var fx = effects[i];
					if (fx == null) continue;
					if (fx is Logic.Scripts.GameDomain.Effects.GrappleEffect) hasGrapple = true;
					else if (fx is Logic.Scripts.GameDomain.Effects.KnockbackEffect) hasKnock = true;
				}
			}

			if (hasGrapple)
			{
				var m = _config.GrappleAreaMaterial;
				if (m != null) { UnityEngine.Debug.Log($"[TelegraphProvider] Using Grapple MESH material: {m.name}"); return m; }
			}
			if (hasKnock)
			{
				var m = _config.KnockbackAreaMaterial;
				if (m != null) { UnityEngine.Debug.Log($"[TelegraphProvider] Using Knockback MESH material: {m.name}"); return m; }
			}
			var fallback = _config.NormalAreaMaterial != null ? _config.NormalAreaMaterial : new Material(Shader.Find("Sprites/Default"));
			UnityEngine.Debug.Log($"[TelegraphProvider] Using Fallback Normal MESH material: {(fallback != null ? fallback.name : "NULL")}");
			return fallback;
		}
	}
}

