using System.Threading.Tasks;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	/// <summary>
	/// Synchronizes <see cref="Turns.TurnFlowController.StartTurns"/> with the Laki roulette board intro
	/// (neutral tiles → delay → reroll). Scene: <see cref="LakiArenaBossBootstrap"/> calls
	/// <see cref="EnsurePendingIntro"/> in Awake and completes the gate after the intro sequence.
	/// </summary>
	public static class LakiRouletteArenaFightIntro
	{
		private static TaskCompletionSource<bool> _introDone;

		public static void EnsurePendingIntro()
		{
			if (_introDone == null)
				_introDone = new TaskCompletionSource<bool>();
		}

		public static Task WaitForBoardIntroIfNeededAsync()
		{
			if (_introDone == null)
				return Task.CompletedTask;
			return _introDone.Task;
		}

		public static void SignalBoardIntroComplete()
		{
			var t = _introDone;
			_introDone = null;
			t?.TrySetResult(true);
		}

		/// <summary>Unblocks <see cref="WaitForBoardIntroIfNeededAsync"/> if turns stop before the intro finishes.</summary>
		public static void CancelWait()
		{
			var t = _introDone;
			_introDone = null;
			t?.TrySetResult(false);
		}
	}
}
