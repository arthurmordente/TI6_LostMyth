using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Telegraph
{
	[CreateAssetMenu(fileName = "TelegraphMaterialConfig", menuName = "Scriptable Objects/Telegraph Material Config")]
	public class TelegraphMaterialConfig : ScriptableObject
	{
		// Legacy area materials (kept for backward compatibility). Used as fallback for Mesh materials.
		public Material NormalAreaMaterial;
		public Material GrappleAreaMaterial;
		public Material KnockbackAreaMaterial;

		// Explicit materials for LineRenderer telegraphs (outlines/rings/lines)
		public Material NormalLineMaterial;
		public Material GrappleLineMaterial;
		public Material KnockbackLineMaterial;
	}
}

