using System;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice
{
	public static class DiceUiRuntime
	{
		public static Action<System.Collections.Generic.List<int>, int, System.Collections.Generic.List<int>, int> OnProgress;
		public static Action<int, int> OnFinalAnimation;
		public static Action OnReset;

		public static void ReportProgress(System.Collections.Generic.List<int> playerRolls, int playerSum, System.Collections.Generic.List<int> bossRolls, int bossSum)
		{
			OnProgress?.Invoke(playerRolls, playerSum, bossRolls, bossSum);
		}

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

