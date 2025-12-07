namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames
{
	public static class MinigameRuntimeService
	{
		private static int _activeCount;
		private static bool _skipOnceOnBossTurn;
		private static bool _pauseBossOnce;
		public interface IMinigameStatusProvider { string GetStatus(); }
		public static IMinigameStatusProvider StatusProvider { get; set; }
		private static readonly System.Collections.Generic.List<IMinigameResolver> _resolvers = new System.Collections.Generic.List<IMinigameResolver>(2);

		public static bool IsActive => _activeCount > 0;

		public static bool ConsumeSkipOnBossTurn()
		{
			if (_skipOnceOnBossTurn)
			{
				_skipOnceOnBossTurn = false;
				return true;
			}
			return false;
		}

		public static bool ConsumePauseBossThisTurn()
		{
			if (_pauseBossOnce)
			{
				_pauseBossOnce = false;
				return true;
			}
			return false;
		}

		public static void Begin()
		{
			_activeCount++;
			UnityEngine.Debug.Log($"[Laki] MinigameRuntime: Begin (active={_activeCount})");
		}

		public static void EndAndScheduleBossResolutionSkip()
		{
			if (_activeCount > 0) _activeCount--;
			_skipOnceOnBossTurn = true;
			_pauseBossOnce = true;
			if (_activeCount <= 0) StatusProvider = null;
			UnityEngine.Debug.Log($"[Laki] MinigameRuntime: End (active={_activeCount}) -> will skip next boss prep");
		}

		public static void RegisterResolver(IMinigameResolver r)
		{
			if (r == null) return;
			if (!_resolvers.Contains(r)) _resolvers.Add(r);
			UnityEngine.Debug.Log($"[Laki] MinigameRuntime: Resolver registered (count={_resolvers.Count})");
		}
		public static void UnregisterResolver(IMinigameResolver r)
		{
			if (r == null) return;
			_resolvers.Remove(r);
			UnityEngine.Debug.Log($"[Laki] MinigameRuntime: Resolver unregistered (count={_resolvers.Count})");
		}

		public static bool TryResolveAnyAtBossTurn(out MinigameResult result, out IMinigameResolver resolver)
		{
			UnityEngine.Debug.Log($"[Laki] MinigameRuntime: TryResolveAnyAtBossTurn resolvers={_resolvers.Count}");
			for (int i = 0; i < _resolvers.Count; i++)
			{
				var r = _resolvers[i];
				if (r != null)
				{
					bool ok = r.TryResolveAtBossTurn(out result);
					UnityEngine.Debug.Log($"[Laki] MinigameRuntime: Resolver[{i}] -> {(ok ? "RESOLVED" : "pending")}");
					if (ok)
					{
						resolver = r;
						return true;
					}
				}
			}
			result = default;
			resolver = null;
			return false;
		}

		public static void Reset()
		{
			_activeCount = 0;
			_skipOnceOnBossTurn = false;
			_pauseBossOnce = false;
			StatusProvider = null;
			_resolvers.Clear();
			UnityEngine.Debug.Log("[Laki] MinigameRuntime: RESET");
		}
	}
}


