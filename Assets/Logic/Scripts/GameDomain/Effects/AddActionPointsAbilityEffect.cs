using System;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Effects
{
	[Serializable]
	public sealed class AddActionPointsAbilityEffect : AbilityEffect
	{
		public int amount = 1;

		public override void Execute(IEffectable caster, IEffectable target)
		{
			int delta = Mathf.Max(1, amount);
			if (target is IEffectableAction act)
			{
				act.AddActionPoints(delta);
				return;
			}
			if (target is INaraController nara)
				nara.AddActionPoints(delta);
		}
	}
}


