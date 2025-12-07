namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames
{
	public interface IMinigameResolver
	{
		bool TryResolveAtBossTurn(out MinigameResult result);
		void DestroyMinigameRoot();
	}
}


