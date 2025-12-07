using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames;
using Logic.Scripts.Turns;
using Logic.Scripts.GameDomain.MVC.Nara;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Diamond
{
	public class DiamondMinigameRound : MonoBehaviour, IMinigameRound, Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.IMinigameResolver
	{
		[SerializeField] private int _chipCost = 1;
		[SerializeField] private int _rounds = 3;
		[SerializeField] private int _diamondsPerRound = 1;
		[SerializeField] private int _bossWinsOnExplosions = 2;
		[SerializeField] private int _diamondHp = 10;
		[SerializeField] private GameObject _diamondPrefab;

		private TurnStateService _turnState;
		private IEnvironmentActorsRegistry _envReg;
		private Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView _arenaView;
		private INaraController _player;

		private int _spawnedRoundIndex;
		private int _explodedCount;
		private int _destroyedCount;
		private bool _cancelled;
		private System.Collections.Generic.List<DiamondActor> _spawnedDiamonds;
		private IEnvironmentTurnActor _progressActor;
		private bool _resolved;
		private MinigameResult _finalResult;

		public int ChipCost => _chipCost;
		public int MaxTurnsToResolve => _rounds;

		public Task<MinigameResult> StartAsync(TurnStateService turnState, IEnvironmentActorsRegistry envRegistry,
			Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay bossEffectable,
			Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView arenaView,
			INaraController player, Logic.Scripts.GameDomain.MVC.Boss.IBossController boss)
		{
			UnityEngine.Debug.Log($"[Laki] DiamondMinigame: start rounds={_rounds} perRound={_diamondsPerRound} hp={_diamondHp} cost={_chipCost}");
			_turnState = turnState;
			_envReg = envRegistry ?? Logic.Scripts.Turns.EnvironmentActorsRegistryService.Instance;
			if (_envReg == null) UnityEngine.Debug.LogWarning("[Laki] DiamondMinigame: EnvironmentActorsRegistry is NULL, diamonds won't explode!");
			_arenaView = arenaView ?? UnityEngine.Object.FindFirstObjectByType<Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView>();
			if (_arenaView == null) UnityEngine.Debug.LogWarning("[Laki] DiamondMinigame: Arena view is NULL, spawn will be deferred!");
			_player = player;

			_spawnedRoundIndex = 0;
			_explodedCount = 0;
			_destroyedCount = 0;
			_cancelled = false;
			_spawnedDiamonds = new System.Collections.Generic.List<DiamondActor>(8);
			_resolved = false;
			_finalResult = default;

			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.StatusProvider = new StatusProvider(this);
			if (_arenaView != null) SpawnDiamondsForRound(_spawnedRoundIndex);
			if (_envReg != null) {
				_progressActor = new DiamondRoundProgressActor(this);
				_envReg.Add(_progressActor);
			}
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.Begin();
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.RegisterResolver(this);

			return System.Threading.Tasks.Task.FromResult(default(MinigameResult));
		}

		public void Cancel() { _cancelled = true; }

		public bool TryResolveAtBossTurn(out MinigameResult result)
		{
			if (_resolved)
			{
				result = _finalResult;
				return true;
			}
			result = default;
			if (_cancelled || _turnState == null) return false;
			bool roundsFinished = (_spawnedRoundIndex + 1) >= _rounds;
			bool bossWins = _explodedCount >= _bossWinsOnExplosions;
			bool playerWins = roundsFinished && !bossWins;
			if (!bossWins && !playerWins)
			{
				int active = CountActiveDiamonds();
				UnityEngine.Debug.Log($"[Laki] DiamondMinigame: resolve check bossTurn; round={_spawnedRoundIndex+1}/{_rounds} exp={_explodedCount}/{_bossWinsOnExplosions} dest={_destroyedCount} activeDiamonds={active}");
				if (roundsFinished && active == 0)
				{
					playerWins = true;
				}
			}
			if (!bossWins && !playerWins) return false;
			UnityEngine.Debug.Log($"[Laki] DiamondMinigame: resolved bossWins={bossWins} playerWins={playerWins} exp={_explodedCount} dest={_destroyedCount} round={_spawnedRoundIndex+1}/{_rounds}");
            int pot = _chipCost * 2;
            result = new MinigameResult
            {
                Completed = true,
                PlayerWon = playerWins && !bossWins,
                BossChipsDelta = (bossWins && !playerWins) ? pot : 0,
                PlayerChipsDelta = (playerWins && !bossWins) ? pot : 0
            };
			return true;
		}

		public void DestroyMinigameRoot()
		{
			_cancelled = true;
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.UnregisterResolver(this);
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.EndAndScheduleBossResolutionSkip();
			if (_envReg != null && _progressActor != null) {
				_envReg.Remove(_progressActor);
			}
			if (_spawnedDiamonds != null) {
				for (int i = 0; i < _spawnedDiamonds.Count; i++) {
					var d = _spawnedDiamonds[i];
					if (d == null) continue;
					try { _envReg?.Remove(d); } catch { }
					try { if (d != null) Destroy(d.gameObject); } catch { }
				}
				_spawnedDiamonds.Clear();
			}
			Destroy(gameObject);
		}

		internal void SpawnDiamondsForRound(int roundIndex)
		{
			if (_arenaView == null) return;
			Vector3 center = _arenaView.transform.position;
			System.Random rng = new System.Random((roundIndex + 1) * 7919 + 37);
			for (int i = 0; i < _diamondsPerRound; i++)
			{
				int tileIndex = rng.Next(0, _arenaView.TileCount);
				Vector3 pos = _arenaView.GetTileWorldCenter(tileIndex);
				GameObject go;
				if (_diamondPrefab != null)
				{
					go = Instantiate(_diamondPrefab, pos, Quaternion.identity);
					go.name = $"Diamond_R{roundIndex}_#{i}";
				}
				else
				{
					go = new GameObject($"Diamond_R{roundIndex}_#{i}");
					go.transform.position = pos;
				}
				var actor = go.GetComponent<DiamondActor>();
				if (actor == null) actor = go.AddComponent<DiamondActor>();
				actor.Init(new Callbacks(this), _envReg, _diamondHp, center);
				_envReg?.Add(actor);
				_spawnedDiamonds.Add(actor);
				UnityEngine.Debug.Log($"[Laki] DiamondMinigame: spawned diamond round={roundIndex} tile={tileIndex} pos=({pos.x:0.0},{pos.y:0.0},{pos.z:0.0})");
			}
		}

		private void OnDiamondExploded() { _explodedCount++; UnityEngine.Debug.Log($"[Laki] DiamondMinigame: explosions={_explodedCount}"); }
		private void OnDiamondDestroyed() { _destroyedCount++; UnityEngine.Debug.Log($"[Laki] DiamondMinigame: destroyed={_destroyedCount}"); }

		private int CountActiveDiamonds()
		{
			if (_envReg == null) return 0;
			var snapshot = _envReg.Snapshot();
			if (snapshot == null || snapshot.Count == 0) return 0;
			int active = 0;
			for (int i = 0; i < snapshot.Count; i++)
			{
				var a = snapshot[i];
				if (a is DiamondActor) active++;
			}
			return active;
		}

		private bool TryResolveFromEnvironment()
		{
			if (_cancelled || _resolved) return false;
			bool roundsFinished = (_spawnedRoundIndex + 1) >= _rounds;
			bool bossWins = _explodedCount >= _bossWinsOnExplosions;
			bool playerWins = roundsFinished && !bossWins;
			int active = CountActiveDiamonds();
			if (!bossWins && !playerWins && roundsFinished && active == 0)
			{
				playerWins = true;
			}
			UnityEngine.Debug.Log($"[Laki] DiamondMinigame: env-check round={_spawnedRoundIndex+1}/{_rounds} exp={_explodedCount}/{_bossWinsOnExplosions} dest={_destroyedCount} activeDiamonds={active} -> bw={bossWins} pw={playerWins}");
			if (!bossWins && !playerWins) return false;
			if (_resolved) return true;
			int pot = _chipCost * 2;
			_finalResult = new MinigameResult
			{
				Completed = true,
				PlayerWon = playerWins,
				BossChipsDelta = playerWins ? 0 : pot,
				PlayerChipsDelta = playerWins ? pot : 0
			};
			_resolved = true;
			UnityEngine.Debug.Log($"[Laki] DiamondMinigame: RESOLUTION SCHEDULED PlayerWon={_finalResult.PlayerWon} P+={_finalResult.PlayerChipsDelta} B+={_finalResult.BossChipsDelta} (will resolve on next BossAct)");
			return true;
		}



		private class DiamondRoundProgressActor : IEnvironmentTurnActor, Logic.Scripts.Turns.IEnvironmentProgressActor
		{
			private readonly DiamondMinigameRound _owner;
			public DiamondRoundProgressActor(DiamondMinigameRound owner) { _owner = owner; }
			public bool RemoveAfterRun => false;
			public async Task ExecuteAsync()
			{
				if (_owner == null || _owner._cancelled) return;
				int current = _owner._turnState != null ? _owner._turnState.TurnNumber : 0;
				bool scheduled = _owner.TryResolveFromEnvironment();
				if (scheduled) { await Task.CompletedTask; return; }
				if (_owner._spawnedRoundIndex + 1 < _owner._rounds)
				{
					_owner._spawnedRoundIndex++;
					_owner.SpawnDiamondsForRound(_owner._spawnedRoundIndex);
				}
				await Task.CompletedTask;
			}
		}

		private readonly struct Callbacks : IDiamondCallbacks
		{
			private readonly DiamondMinigameRound _o;
			public Callbacks(DiamondMinigameRound o) { _o = o; }
			public void OnDiamondExploded() { _o?.OnDiamondExploded(); }
			public void OnDiamondDestroyed() { _o?.OnDiamondDestroyed(); }
		}

		private sealed class StatusProvider : Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.IMinigameStatusProvider
		{
			private readonly DiamondMinigameRound _o;
			public StatusProvider(DiamondMinigameRound o) { _o = o; }
			public string GetStatus()
			{
				if (_o == null) return "Diamonds: (n/a)";
				return $"Diamonds round={_o._spawnedRoundIndex + 1}/{_o._rounds} exploded={_o._explodedCount} destroyed={_o._destroyedCount}";
			}
		}
	}
}


