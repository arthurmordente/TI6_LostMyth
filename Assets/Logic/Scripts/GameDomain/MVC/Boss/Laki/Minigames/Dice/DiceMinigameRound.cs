using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.Turns;
using Logic.Scripts.GameDomain.MVC.Nara;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice
{
	public class DiceMinigameRound : MonoBehaviour, IMinigameRound, Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.IMinigameResolver, IDiceCallbacks
	{
		[SerializeField] private string _minigameName = "Dice";
		[SerializeField] private int _chipCost = 2;
		[SerializeField] private int _rounds = 3;
		[SerializeField] private int _maxValue = 6;
		[SerializeField] private int _dieHp = 99;
		[SerializeField] private GameObject _playerDiePrefab;
		[SerializeField] private GameObject _bossDiePrefab;
		[SerializeField] private bool _playerWinsOnTie;

		private TurnStateService _turnState;
		private IEnvironmentActorsRegistry _envReg;
		private Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView _arenaView;
		private INaraController _player;
		private Logic.Scripts.GameDomain.MVC.Boss.IBossController _boss;

		private int _roundIndex;
		private int _playerScore;
		private int _bossScore;
		private bool _playerRolled;
		private bool _bossRolled;
		private System.Collections.Generic.List<int> _playerRolls;
		private System.Collections.Generic.List<int> _bossRolls;

		private bool _resolved;
		private MinigameResult _final;
		private IEnvironmentTurnActor _progressActor;
		private int _curPlayerValue;
		private int _curBossValue;

		public string MinigameName => _minigameName;
		public int ChipCost => _chipCost;
		public int MaxTurnsToResolve => _rounds;

		public Task<MinigameResult> StartAsync(TurnStateService turnState, IEnvironmentActorsRegistry envRegistry,
			Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay bossEffectable,
			Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView arenaView,
			INaraController player, Logic.Scripts.GameDomain.MVC.Boss.IBossController boss)
		{
			_turnState = turnState;
			_envReg = envRegistry ?? Logic.Scripts.Turns.EnvironmentActorsRegistryService.Instance;
			_arenaView = arenaView ?? UnityEngine.Object.FindFirstObjectByType<Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView>();
			_player = player;
			_boss = boss;

			_roundIndex = 0;
			_playerScore = 0;
			_bossScore = 0;
			_playerRolled = false;
			_bossRolled = false;
			_resolved = false;
			_final = default;
			_playerRolls = new System.Collections.Generic.List<int>(_rounds);
			_bossRolls = new System.Collections.Generic.List<int>(_rounds);
			_curPlayerValue = 0;
			_curBossValue = 0;

			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.StatusProvider = new Status(this);
			if (_arenaView != null) SpawnRoundDice(_roundIndex);
			if (_envReg != null)
			{
				_progressActor = new Progress(this);
				_envReg.Add(_progressActor);
			}
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.Begin();
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.RegisterResolver(this);
			return Task.FromResult(default(MinigameResult));
		}

		public void Cancel() { _resolved = true; }

		public bool TryResolveAtBossTurn(out MinigameResult result)
		{
			if (_resolved) { result = _final; return true; }
			result = default;
			return false;
		}

		public void DestroyMinigameRoot()
		{
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.UnregisterResolver(this);
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.EndAndScheduleBossResolutionSkip();
			if (_envReg != null && _progressActor != null) { _envReg.Remove(_progressActor); }
			Destroy(gameObject);
		}

		public void OnDiceRolled(bool isBoss, int value)
		{
			if (isBoss) { _bossScore += value; _bossRolled = true; _bossRolls.Add(value); _curBossValue = 0; }
			else { _playerScore += value; _playerRolled = true; _playerRolls.Add(value); _curPlayerValue = 0; }
			ReportUiProgress();
		}

		private void SpawnRoundDice(int roundIdx)
		{
			if (_arenaView == null) return;
			System.Random rng = new System.Random((roundIdx + 1) * 7919 + 137);
			int tileB = rng.Next(0, _arenaView.TileCount);
			Vector3 bossSpawn = GetBossPosition();
			Vector3 bPos = _arenaView.GetTileWorldCenter(tileB);
			// player target constrained near player's current sector (±3 sectors)
			Vector3 playerPos = GetPlayerPosition();
			int near = NearestTileIndex(playerPos);
			int bands = 2;
			int sectorCount = Mathf.Max(1, _arenaView.TileCount / bands);
			int band = near % bands;
			int sector = near / bands;
			int offset = Random.Range(-3, 4);
			int newSector = (sector + offset + sectorCount) % sectorCount;
			int tileP = newSector * bands + band;
			Vector3 pSpawn = playerPos;
			Vector3 pPos = _arenaView.GetTileWorldCenter(tileP);

			GameObject pGo = _playerDiePrefab != null ? Instantiate(_playerDiePrefab, pSpawn, Quaternion.identity) : new GameObject("PlayerDie");
			if (_playerDiePrefab == null) pGo.transform.position = pSpawn;
			GameObject bGo = _bossDiePrefab != null ? Instantiate(_bossDiePrefab, bossSpawn, Quaternion.identity) : new GameObject("BossDie");
			if (_bossDiePrefab == null) bGo.transform.position = bossSpawn;

			int initP = UnityEngine.Random.Range(1, _maxValue + 1);
			int initB = UnityEngine.Random.Range(1, _maxValue + 1);

			var pActor = pGo.GetComponent<DiceActor>(); if (pActor == null) pActor = pGo.AddComponent<DiceActor>();
			pActor.Init(this, false, _maxValue, _dieHp, initP, _arenaView, tileP, pSpawn);
			var bActor = bGo.GetComponent<DiceActor>(); if (bActor == null) bActor = bGo.AddComponent<DiceActor>();
			bActor.Init(this, true, _maxValue, _dieHp, initB, _arenaView, tileB, bossSpawn);

			_envReg?.Add(pActor);
			_envReg?.Add(bActor);

			_playerRolled = false;
			_bossRolled = false;
			_curPlayerValue = initP;
			_curBossValue = initB;
		}

		private int NearestTileIndex(Vector3 pos)
		{
			if (_arenaView == null) return 0;
			int bestIdx = 0;
			float best = float.MaxValue;
			for (int i = 0; i < _arenaView.TileCount; i++)
			{
				Vector3 c = _arenaView.GetTileWorldCenter(i);
				float d = (c - pos).sqrMagnitude;
				if (d < best) { best = d; bestIdx = i; }
			}
			return bestIdx;
		}

		private Vector3 GetBossPosition()
		{
			Vector3 fallback = (_arenaView != null) ? _arenaView.transform.position : Vector3.zero;
			var bc = _boss as Logic.Scripts.GameDomain.MVC.Boss.BossController;
			if (bc != null)
			{
				try
				{
					var t = bc.GetReferenceTransform();
					if (t != null) { Vector3 p = t.position; return p; }
				} catch { }
			}
			return fallback;
		}

		private Vector3 GetPlayerPosition()
		{
			Vector3 fallback = (_arenaView != null) ? _arenaView.transform.position : Vector3.zero;
			if (_player != null)
			{
				try {
					var go = _player.NaraViewGO;
					if (go != null) { Vector3 p = go.transform.position; p.y = p.y + 2f; return p; }
				} catch { }
			}
			fallback.y = fallback.y + 2f;
			return fallback;
		}

		private bool TryResolveFromEnvironment()
		{
			if (_resolved) return false;
			bool roundsFinished = (_roundIndex + 1) >= _rounds;
			if (!roundsFinished) return false;
			int pot = _chipCost * 2;
			bool playerWon = _playerScore > _bossScore || (_playerScore == _bossScore && _playerWinsOnTie);
			_final = new MinigameResult
			{
				Completed = true,
				PlayerWon = playerWon,
				BossChipsDelta = playerWon ? 0 : pot,
				PlayerChipsDelta = playerWon ? pot : 0
			};
			_resolved = true;
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.ReportFinal(_playerScore, _bossScore);
			return true;
		}

		public void OnDieValueChanged(bool isBoss, int value)
		{
			if (isBoss) _curBossValue = value; else _curPlayerValue = value;
			// Do not update UI here to avoid spoiling the roll; wait for animation complete
		}

		public void OnDieAnimationComplete(bool isBoss, int value)
		{
			if (isBoss) _curBossValue = value; else _curPlayerValue = value;
			ReportUiProgress();
		}

		private void ReportUiProgress()
		{
			var pList = new System.Collections.Generic.List<int>(_playerRolls);
			if (_curPlayerValue > 0) pList.Add(_curPlayerValue);
			var bList = new System.Collections.Generic.List<int>(_bossRolls);
			if (_curBossValue > 0) bList.Add(_curBossValue);
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.ReportProgress(pList, Sum(pList), bList, Sum(bList));
		}

		private static int Sum(System.Collections.Generic.List<int> l)
		{
			int s = 0;
			if (l != null) for (int i = 0; i < l.Count; i++) s += l[i];
			return s;
		}

		private class Progress : IEnvironmentTurnActor, Logic.Scripts.Turns.IEnvironmentProgressActor
		{
			private readonly DiceMinigameRound _o;
			public Progress(DiceMinigameRound o) { _o = o; }
			public bool RemoveAfterRun => false;
			public async Task ExecuteAsync()
			{
				if (_o == null || _o._resolved) return;
				if (_o._playerRolled && _o._bossRolled)
				{
					if (_o._roundIndex + 1 < _o._rounds)
					{
						_o._roundIndex++;
						_o.SpawnRoundDice(_o._roundIndex);
					}
					else
					{
						_o.TryResolveFromEnvironment();
					}
				}
				await Task.CompletedTask;
			}
		}

		private sealed class Status : Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.IMinigameStatusProvider
		{
			private readonly DiceMinigameRound _o;
			public Status(DiceMinigameRound o) { _o = o; }
			public string GetStatus()
			{
				if (_o == null) return "Dice: (n/a)";
				return $"Dice round={_o._roundIndex + 1}/{_o._rounds} score P={_o._playerScore} B={_o._bossScore}";
			}
		}
	}
}

