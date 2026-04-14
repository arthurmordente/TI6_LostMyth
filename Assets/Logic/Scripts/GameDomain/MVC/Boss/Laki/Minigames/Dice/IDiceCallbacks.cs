using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice
{
	public interface IDiceCallbacks
	{
		/// <param name="rollSlotIndex">Per-side die index (0..n-1) so rerolls update the correct entry.</param>
		void OnDiceRolled(bool isBoss, int rollSlotIndex, int value);
		void OnDieValueChanged(bool isBoss, int rollSlotIndex, int value);
		void OnDieAnimationComplete(bool isBoss, int rollSlotIndex, int value);
	}
}

