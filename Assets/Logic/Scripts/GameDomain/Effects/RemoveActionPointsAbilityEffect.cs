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
			Debug.Log(
				$"[LakiTileEffect][RemoveAP] Execute amount={amount} " +
				$"target={(target != null ? target.GetType().Name : "null")} " +
				$"caster={(caster != null ? caster.GetType().Name : "null")}");

			if (target is IEffectableAction act)
			{
				Debug.Log($"[LakiTileEffect][RemoveAP] SubtractActionPoints({amount}) on {act.GetType().Name}");
				act.SubtractActionPoints(amount);
				return;
			}

			if (target is INaraController nara)
			{
				Debug.Log($"[LakiTileEffect][RemoveAP] Target is INaraController — routing SubtractActionPoints({amount})");
				nara.SubtractActionPoints(amount);
				return;
			}

			Debug.LogWarning(
				$"[LakiTileEffect][RemoveAP] Ignored — target does not implement IEffectableAction/INaraController " +
				$"(type={(target != null ? target.GetType().Name : "null")}).");
		}
	}
}
