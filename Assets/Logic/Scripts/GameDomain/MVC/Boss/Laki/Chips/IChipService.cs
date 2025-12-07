namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips
{
	public interface IChipService
	{
		int PlayerChips { get; }
		int BossChips { get; }
		int HpPerChip { get; }
		void SetInitial(int player, int boss);
		void SetHpPerChip(int hpPerChip);
		void ApplyMinigameResult(Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameResult result);
		System.Action<int,int> OnChipsChanged { get; set; }
		System.Action<int,int> OnBetPlaced { get; set; }
		System.Action<bool,int> OnPotResolve { get; set; }
		System.Action<bool,int,int> OnChipPurchased { get; set; }
		void Refresh();
		bool TryPayPlayer(Logic.Scripts.GameDomain.MVC.Nara.INaraController player, int cost, out int hpConverted);
		bool TryPayBoss(Logic.Scripts.GameDomain.MVC.Boss.IBossController boss, int cost, out int hpConverted);
	}
}

