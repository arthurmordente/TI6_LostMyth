using System;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Effects
{
	[Serializable]
	public sealed class RemoveActionPointsAbilityEffect : AbilityEffect
	{
		public int amount = 1;

		public override void Execute(IEffectable caster, IEffectable target)
		{
			int delta = Mathf.Max(1, amount);

			if (target is IEffectableAction act)
			{
				act.SubtractActionPoints(delta);
				return;
			}

			if (target is INaraController nara)
			{
				nara.SubtractActionPoints(delta);
				return;
			}

			Debug.LogWarning(
				$"[RemoveActionPointsAbilityEffect] Ignorado — alvo não implementa IEffectableAction/INaraController " +
				$"(tipo={(target != null ? target.GetType().Name : "null")}).");
		}
	}
}
