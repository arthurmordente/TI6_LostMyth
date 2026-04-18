using System;
using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice
{
	public static class DiceUiRuntime
	{
		public static Action<DiceUiProgressPayload> OnProgress;
		public static Action<int, int> OnFinalAnimation;
		public static Action OnReset;

		public static void ReportProgress(DiceUiProgressPayload payload)
		{
			if (payload == null) return;
			OnProgress?.Invoke(payload);
		}

		public static void ReportProgress(List<int> playerRolls, int playerSum, List<int> bossRolls, int bossSum) =>
			ReportProgress(DiceUiProgressPayload.Quiet(playerRolls, playerSum, bossRolls, bossSum));

		public static void ReportFinal(int playerSum, int bossSum)
		{
			OnFinalAnimation?.Invoke(playerSum, bossSum);
		}

		public static void Reset()
		{
			OnReset?.Invoke();
		}
	}
}

