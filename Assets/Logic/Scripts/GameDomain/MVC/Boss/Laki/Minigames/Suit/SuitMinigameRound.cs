using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.Turns;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Environment.Laki;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Suit
{
	/// <summary>
	/// Minigame de Memória / Naipe.
	/// Custo: 3 fichas. Duração: 3–4 rodadas.
	///
	/// Início: todas as casas revelam seu naipe (1–4) com animação de flash, depois escondem.
	/// A cada rodada (EnviromentAct):
	///   - As casas do jogador e do Livro (EchoView) revelam seus naipes.
	///   - O efeito fixo do minigame (positivo/negativo) é aplicado conforme a cor da casa.
	///   - Se jogador e Livro estiverem em casas do MESMO naipe → rodada passa, velocidade aumenta.
	///   - Caso contrário → jogador perde.
	/// Se todas as rodadas forem superadas → jogador ganha o pote.
	/// </summary>
	public class SuitMinigameRound : MonoBehaviour, IMinigameRound, IMinigameResolver
	{
		[SerializeField] private string _minigameName = "Naipe";
		[SerializeField] private int _chipCost = 3;
		[SerializeField] private int _rounds = 3;
		[SerializeField, Range(200, 2000)] private int _baseRevealMs = 800;
		[SerializeField, Range(100, 1500)] private int _holdMs = 1000;
		[SerializeField, Range(0.3f, 1f)] private float _speedMultiplierPerRound = 0.75f;

		[SerializeReference]
		private List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _fixedPositiveEffects
			= new List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect>();

		[SerializeReference]
		private List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _fixedNegativeEffects
			= new List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect>();

		// ─── Runtime state ────────────────────────────────────────────────────────

		private TurnStateService _turnState;
		private IEnvironmentActorsRegistry _envReg;
		private LakiRouletteArenaView _arenaView;
		private INaraController _player;
		private IEffectable _caster;

		private int[] _tileSuits;
		private int _roundIndex;
		private float _currentRevealMs;
		private bool _resolved;
		private MinigameResult _finalResult;
		private IEnvironmentTurnActor _progressActor;
		private bool _cancelled;

		// ─── IMinigameRound ───────────────────────────────────────────────────────

		public string MinigameName => _minigameName;
		public int ChipCost => _chipCost;
		public int MaxTurnsToResolve => _rounds;

		public async Task<MinigameResult> StartAsync(
			TurnStateService turnState,
			IEnvironmentActorsRegistry envRegistry,
			Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay bossEffectable,
			LakiRouletteArenaView arenaView,
			INaraController player,
			IBossController boss)
		{
			_turnState = turnState;
			_envReg = envRegistry ?? EnvironmentActorsRegistryService.Instance;
			_arenaView = arenaView ?? Object.FindFirstObjectByType<LakiRouletteArenaView>();
			_player = player;
			_caster = bossEffectable as IEffectable;

			_roundIndex = 0;
			_currentRevealMs = _baseRevealMs;
			_resolved = false;
			_cancelled = false;
			_finalResult = default;

			MinigameRuntimeService.StatusProvider = new StatusProvider(this);
			MinigameRuntimeService.SetActiveName(_minigameName);

			if (_arenaView != null)
			{
				_arenaView.InitSuitOverlay();
				int seed = (_turnState != null ? _turnState.TurnNumber : 0) * 2741 + 3;
				AssignRandomSuits(new System.Random(seed));

				// Reveal all suits before the player acts (blocks BossAct briefly)
				await _arenaView.AnimateSuitRevealAsync(
					_tileSuits,
					Mathf.RoundToInt(_currentRevealMs),
					_holdMs);
			}

			if (_envReg != null)
			{
				_progressActor = new SuitProgressActor(this);
				_envReg.Add(_progressActor);
			}

			MinigameRuntimeService.Begin();
			MinigameRuntimeService.RegisterResolver(this);

			Debug.Log($"[Laki][Suit] Started – rounds={_rounds} revealMs={_currentRevealMs:0} cost={_chipCost}");
			return default;
		}

		public void Cancel() => _cancelled = true;

		// ─── IMinigameResolver ────────────────────────────────────────────────────

		public bool TryResolveAtBossTurn(out MinigameResult result)
		{
			if (_resolved) { result = _finalResult; return true; }
			result = default;
			return false;
		}

		public void DestroyMinigameRoot()
		{
			_cancelled = true;
			MinigameRuntimeService.UnregisterResolver(this);
			MinigameRuntimeService.EndAndScheduleBossResolutionSkip();
			if (_envReg != null && _progressActor != null) _envReg.Remove(_progressActor);
			_arenaView?.DestroySuitOverlay();
			Destroy(gameObject);
		}

		// ─── Helpers ──────────────────────────────────────────────────────────────

		private void AssignRandomSuits(System.Random rng)
		{
			if (_arenaView == null) return;
			int count = _arenaView.TileCount;
			if (_tileSuits == null || _tileSuits.Length != count)
				_tileSuits = new int[count];
			for (int i = 0; i < count; i++)
				_tileSuits[i] = rng.Next(1, 5); // 1–4
		}

		private int GetSuitAt(int tileIndex)
		{
			if (_tileSuits == null || tileIndex < 0 || tileIndex >= _tileSuits.Length) return -1;
			return _tileSuits[tileIndex];
		}

		private string ApplyFixedEffect(int tileIndex)
		{
			if (_player == null || _arenaView == null) return "Neutro";
			var effType = _arenaView.GetCachedTileEffect(tileIndex);
			var target = _player as IEffectable;
			if (target == null) return "Neutro";

			switch (effType)
			{
				case RouletteArenaService.TileEffectType.Positive:
					if (_fixedPositiveEffects.Count > 0)
					{
						var eff = _fixedPositiveEffects[0];
						eff?.Execute(_caster, target);
						return eff?.Name ?? "Positivo";
					}
					target.Heal(5);
					return "Heal5";

				case RouletteArenaService.TileEffectType.Negative:
					if (_fixedNegativeEffects.Count > 0)
					{
						var eff = _fixedNegativeEffects[0];
						eff?.Execute(_caster, target);
						return eff?.Name ?? "Negativo";
					}
					target.TakeDamage(5);
					return "Damage5";

				default:
					return "Neutro";
			}
		}

		private static Vector3? FindBookPosition()
		{
			var bookView = Object.FindFirstObjectByType<Logic.Scripts.GameDomain.MVC.Book.BookView>();
			if (bookView != null) return bookView.transform.position;
			return null;
		}

		// ─── Progress actor ───────────────────────────────────────────────────────

		private sealed class SuitProgressActor : IEnvironmentTurnActor
		{
			private readonly SuitMinigameRound _o;
			public SuitProgressActor(SuitMinigameRound o) { _o = o; }
			public bool RemoveAfterRun => false;

			public async Task ExecuteAsync()
			{
				if (_o == null || _o._cancelled || _o._resolved) return;

				int turn = _o._turnState?.TurnNumber ?? 0;

				// Resolve tile positions
				Vector3 playerPos = _o._player?.NaraViewGO != null
					? _o._player.NaraViewGO.transform.position
					: Vector3.zero;
				int playerTile = _o._arenaView != null ? _o._arenaView.ComputeTileIndex(playerPos) : -1;

				Vector3? bookPos = FindBookPosition();
				int bookTile = (bookPos.HasValue && _o._arenaView != null)
					? _o._arenaView.ComputeTileIndex(bookPos.Value)
					: -1;

				// Reveal only the occupied tiles at the moment of effect application
				var toReveal = new HashSet<int>();
				if (playerTile >= 0) toReveal.Add(playerTile);
				if (bookTile >= 0) toReveal.Add(bookTile);

				if (_o._arenaView != null && toReveal.Count > 0)
				{
					await _o._arenaView.AnimateSuitRevealTilesAsync(
						toReveal,
						_o._tileSuits,
						Mathf.RoundToInt(_o._currentRevealMs * 0.5f),
						Mathf.RoundToInt(_o._holdMs * 0.5f));
				}

				// Apply fixed effect based on player tile color and log details
				string appliedEffect = "Neutro";
				if (playerTile >= 0) appliedEffect = _o.ApplyFixedEffect(playerTile);

				int playerSuit = _o.GetSuitAt(playerTile);
				int bookSuit   = _o.GetSuitAt(bookTile);

				bool hasBook = bookPos.HasValue && bookTile >= 0;
				bool sameSuit = hasBook && playerSuit >= 1 && playerSuit == bookSuit;

				// ── Debug log de casas durante o minigame de memória ──
				Debug.Log(
					$"[Laki][Naipe] Rodada={_o._roundIndex + 1}/{_o._rounds} Turno={turn}\n" +
					$"  Jogador → Casa={playerTile}  Naipe={playerSuit}  Efeito={appliedEffect}\n" +
					$"  Livro   → Casa={bookTile}  Naipe={bookSuit}  " +
					$"{(hasBook ? "" : "(NÃO ENCONTRADO)")}\n" +
					$"  Resultado={( sameSuit ? "PASSOU (naipes iguais)" : "FALHOU (naipes diferentes)")}");

				if (!hasBook)
					Debug.LogWarning("[Laki][Naipe] BookView não encontrado na cena – rodada tratada como falha.");

				if (!sameSuit)
				{
					int pot = _o._chipCost * 2;
					_o._finalResult = new MinigameResult
					{
						Completed = true,
						PlayerWon = false,
						BossChipsDelta = pot,
						PlayerChipsDelta = 0
					};
					_o._resolved = true;
					Debug.Log($"[Laki][Suit] FALHA – boss ganha pote={pot}");
					return;
				}

				// Round passed
				_o._roundIndex++;
				if (_o._roundIndex >= _o._rounds)
				{
					int pot = _o._chipCost * 2;
					_o._finalResult = new MinigameResult
					{
						Completed = true,
						PlayerWon = true,
						BossChipsDelta = 0,
						PlayerChipsDelta = pot
					};
					_o._resolved = true;
					Debug.Log($"[Laki][Suit] VITÓRIA – jogador ganha pote={pot}");
					return;
				}

				// Prepare next round: faster reveal with new suits
				_o._currentRevealMs = Mathf.Max(150f, _o._currentRevealMs * _o._speedMultiplierPerRound);
				int seed = (turn + _o._roundIndex) * 7919 + 53;
				_o.AssignRandomSuits(new System.Random(seed));

				if (_o._arenaView != null)
				{
					await _o._arenaView.AnimateSuitRevealAsync(
						_o._tileSuits,
						Mathf.RoundToInt(_o._currentRevealMs),
						Mathf.RoundToInt(_o._holdMs * _o._speedMultiplierPerRound));
				}

				Debug.Log($"[Laki][Suit] Rodada {_o._roundIndex + 1}/{_o._rounds} – novo revealMs={_o._currentRevealMs:0}");
			}
		}

		// ─── Status provider ──────────────────────────────────────────────────────

		private sealed class StatusProvider : MinigameRuntimeService.IMinigameStatusProvider
		{
			private readonly SuitMinigameRound _o;
			public StatusProvider(SuitMinigameRound o) { _o = o; }
			public string GetStatus()
			{
				if (_o == null) return "Naipe: (n/a)";
				return $"Naipe rodada={_o._roundIndex + 1}/{_o._rounds} reveal={_o._currentRevealMs:0}ms";
			}
		}
	}
}
