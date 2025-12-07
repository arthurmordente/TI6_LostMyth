using System.Collections.Generic;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Abilitys;

namespace Logic.Scripts.GameDomain.MVC.Boss.Telegraph
{
	public interface ITelegraphMaterialProvider
	{
		Material GetMaterial(bool telegraphDisplacementEnabled, IList<AbilityEffect> effects);
		Material GetLineMaterial(bool telegraphDisplacementEnabled, IList<AbilityEffect> effects);
		Material GetMeshMaterial(bool telegraphDisplacementEnabled, IList<AbilityEffect> effects);
	}
}

