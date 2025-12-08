using System.Threading.Tasks;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Boss;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames
{
	public interface IMinigameRound
	{
		// Exposed display name for UI
		string MinigameName { get; }
		int ChipCost { get; }
		int MaxTurnsToResolve { get; }
		Task<MinigameResult> StartAsync(Logic.Scripts.Turns.TurnStateService turnState,
			Logic.Scripts.Turns.IEnvironmentActorsRegistry envRegistry,
			Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay bossEffectable,
			Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView arenaView,
			INaraController player, IBossController boss);
		void Cancel();
	}
}


