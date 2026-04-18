using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice
{
	/// <summary>
	/// Snapshot of dice UI state for one <see cref="DiceUiRuntime.OnProgress"/> tick.
	/// Per-slot punch flags come from gameplay (e.g. projectile hit); when null, the panel may infer changes for legacy flows.
	/// </summary>
	public sealed class DiceUiProgressPayload
	{
		public List<int> PlayerRolls;
		public int PlayerSum;
		public List<int> BossRolls;
		public int BossSum;
		/// <summary>Length should match <see cref="PlayerRolls"/>.Count when non-null.</summary>
		public bool[] PlayerSlotPunch;
		public bool[] BossSlotPunch;
		public bool PunchPlayerSum;
		public bool PunchBossSum;

		public static DiceUiProgressPayload Quiet(List<int> playerRolls, int playerSum, List<int> bossRolls, int bossSum) =>
			new DiceUiProgressPayload
			{
				PlayerRolls = playerRolls,
				PlayerSum = playerSum,
				BossRolls = bossRolls,
				BossSum = bossSum,
				PlayerSlotPunch = null,
				BossSlotPunch = null,
				PunchPlayerSum = false,
				PunchBossSum = false
			};
	}
}
