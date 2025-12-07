using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice
{
	public interface IDiceCallbacks
	{
		void OnDiceRolled(bool isBoss, int value);
		void OnDieValueChanged(bool isBoss, int value);
		void OnDieAnimationComplete(bool isBoss, int value);
	}
}

