using System;
using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice
{
	public static class DiceUiRuntime
	{
		public static Action<DiceUiProgressPayload> OnProgress;
		public static Action<int, int> OnFinalAnimation;
		public static Action<int, int, bool, bool> OnScoreboardCelebration;
		public static Action OnReset;

		public static int LastPlayerSum { get; private set; }
		public static int LastBossSum { get; private set; }

		public static void ReportProgress(DiceUiProgressPayload payload)
		{
			if (payload == null) return;
			OnProgress?.Invoke(payload);
		}

		public static void ReportProgress(List<int> playerRolls, int playerSum, List<int> bossRolls, int bossSum) =>
			ReportProgress(DiceUiProgressPayload.Quiet(playerRolls, playerSum, bossRolls, bossSum));

		public static void ReportFinal(int playerSum, int bossSum)
		{
			LastPlayerSum = playerSum;
			LastBossSum = bossSum;
			OnFinalAnimation?.Invoke(playerSum, bossSum);
		}

		public static void RequestScoreboardCelebration(int playerSum, int bossSum, bool playerWon, bool isTie) =>
			OnScoreboardCelebration?.Invoke(playerSum, bossSum, playerWon, isTie);

		public static void Reset()
		{
			LastPlayerSum = 0;
			LastBossSum = 0;
			OnReset?.Invoke();
		}
	}
}

