using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.Turns;
using Logic.Scripts.GameDomain.MVC.Nara;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	public sealed class LakiRouletteArenaActor : IEnvironmentTurnActor
	{
		private readonly ITurnStateReader _turnState;
		private readonly INaraController _nara;
		private readonly RouletteArenaService _arena;
		private readonly IEffectable _caster;
		private readonly IRouletteArenaVisual _visual;
		private readonly IEffectable _bookEffectable;
		private Vector3 _centerWorld;

		public bool RemoveAfterRun => false;

		public LakiRouletteArenaActor(ITurnStateReader turnState, INaraController nara, RouletteArenaService arena, Vector3? centerWorld = null, IRouletteArenaVisual visual = null, IEffectable caster = null, IEffectable bookEffectable = null)
		{
			_turnState = turnState;
			_nara = nara;
			_arena = arena ?? new RouletteArenaService();
			_visual = visual;
			_caster = caster;
			_bookEffectable = bookEffectable;
			_centerWorld = centerWorld ?? new Vector3(0f, 7f, 0f);

			int t = _turnState != null ? _turnState.TurnNumber : 0;
			_arena.RerollTiles(t, new System.Random(t * 7919 + 17));
			_visual?.RefreshFrom(_arena);
		}

		public async Task ExecuteAsync()
		{
			int turn = _turnState != null ? _turnState.TurnNumber : 0;

			Vector3 playerPos = (_nara != null && _nara.NaraViewGO != null) ? _nara.NaraViewGO.transform.position : Vector3.zero;
			int playerTile = _arena.ComputeTileIndex(playerPos, _centerWorld);
			System.Collections.Generic.HashSet<int> tilesToEmphasize = new System.Collections.Generic.HashSet<int>();
			if (playerTile >= 0) tilesToEmphasize.Add(playerTile);

			// Detect the Book's tile for emphasis
			int bookTile = -1;
			try
			{
				var bookView = UnityEngine.Object.FindFirstObjectByType<Logic.Scripts.GameDomain.MVC.Book.BookView>();
				if (bookView != null)
				{
					bookTile = _arena.ComputeTileIndex(bookView.transform.position, _centerWorld);
					if (bookTile >= 0) tilesToEmphasize.Add(bookTile);
				}
			}
			catch { }

			if (_visual != null && tilesToEmphasize.Count > 0)
			{
				int steps = 20;
				for (int i = 0; i <= steps; i++)
				{
					float t = (float)i / steps;
					_visual.SetEmphasis(tilesToEmphasize, t, 0.85f);
					await System.Threading.Tasks.Task.Delay(100);
				}
			}

			// Apply tile effect to the player
			if (playerTile >= 0)
			{
				var type = _arena.GetTileEffect(playerTile);
				string applied = _arena.ApplyEffectToPlayer(_caster, _nara, playerTile, turn);
				UnityEngine.Debug.Log($"[LakiRouletteArena][Jogador] Turn={turn} Tile={playerTile} Type={type} Effect={(applied ?? "None")}");
			}

			// Apply tile effect to the Book separately (so it receives its own tile's effect)
			if (bookTile >= 0 && _bookEffectable != null)
			{
				var btype = _arena.GetTileEffect(bookTile);
				string bapplied = _arena.ApplyEffectToEffectable(_caster, _bookEffectable, bookTile, turn);
				UnityEngine.Debug.Log($"[LakiRouletteArena][Livro] Turn={turn} Tile={bookTile} Type={btype} Effect={(bapplied ?? "None")}");
			}

			await System.Threading.Tasks.Task.Delay(1000);

			for (int i = 0; i < 3; i++)
			{
				_arena.RandomizeVisualMapping(new System.Random((turn + i + 1) * 104729 + playerTile));
				_visual?.RefreshFrom(_arena);
				await System.Threading.Tasks.Task.Delay(150);
			}

			int nextTurn = turn + 1;
			_arena.RerollTiles(nextTurn, new System.Random(nextTurn * 7919 + 17));
			_visual?.RefreshFrom(_arena);

			await Task.CompletedTask;
		}

		public void SetCenter(Vector3 centerWorld)
		{
			_centerWorld = centerWorld;
		}
	}
}


