using System;
using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Turns;
using Logic.Scripts.GameDomain.MVC.Nara;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	public sealed class LakiRouletteArenaActor : IEnvironmentTurnActor, ILakiRouletteArenaTurnPhases
	{
		const int PostEffectsPauseMs = 500;

		private readonly ITurnStateReader _turnState;
		private readonly INaraController _nara;
		private readonly RouletteArenaService _arena;
		private readonly IEffectable _caster;
		private readonly IRouletteArenaVisual _visual;
		private readonly IEffectable _bookEffectable;
		private readonly IAudioService _audioService;
		private Vector3 _centerWorld;

		public bool RemoveAfterRun => false;

		public LakiRouletteArenaActor(
			ITurnStateReader turnState,
			INaraController nara,
			RouletteArenaService arena,
			Vector3? centerWorld = null,
			IRouletteArenaVisual visual = null,
			IEffectable caster = null,
			IEffectable bookEffectable = null,
			IAudioService audioService = null)
		{
			_turnState = turnState;
			_nara = nara;
			_arena = arena ?? new RouletteArenaService();
			_visual = visual;
			_caster = caster;
			_bookEffectable = bookEffectable;
			_audioService = audioService;
			_centerWorld = centerWorld ?? new Vector3(0f, 0.5f, -4f);
		}

		/// <summary>Legacy path when <see cref="LakiArenaTurnFlowBridge"/> is not used.</summary>
		public async Task ExecuteAsync()
		{
			await ExecuteApplyPhaseAsync();
			await Task.Delay(1000);
			await ExecuteRerollPhaseAsync();
		}

		public async Task ExecuteApplyPhaseAsync()
		{
			int turn = _turnState != null ? _turnState.TurnNumber : 0;

			Vector3 playerPos = (_nara != null && _nara.NaraViewGO != null) ? _nara.NaraViewGO.transform.position : Vector3.zero;
			int playerTile = _arena.ComputeTileIndex(playerPos, _centerWorld);
			var tilesToEmphasize = new System.Collections.Generic.HashSet<int>();
			if (playerTile >= 0) tilesToEmphasize.Add(playerTile);

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
				const int steps = 20;
				for (int i = 0; i <= steps; i++)
				{
					float t = (float)i / steps;
					_visual.SetEmphasis(tilesToEmphasize, t, 0.85f);
					await Task.Delay(100);
				}
			}

			if (playerTile >= 0)
			{
				LakiTileEffectApplyDebug.LogUnitOnTile(
					"Player", turn, playerTile, _arena.GetTileEffect(playerTile), playerPos);
				await _arena.ApplyEffectToPlayerAsync(_caster, _nara, playerTile, turn);
			}
			else
			{
				LakiTileEffectApplyDebug.LogUnitOnTile("Player", turn, playerTile, default, playerPos);
			}

			if (bookTile >= 0 && _bookEffectable != null)
			{
				Vector3 bookPos = _bookEffectable.GetReferenceTransform() != null
					? _bookEffectable.GetReferenceTransform().position
					: Vector3.zero;
				LakiTileEffectApplyDebug.LogUnitOnTile(
					"Clone", turn, bookTile, _arena.GetTileEffect(bookTile), bookPos);
				await _arena.ApplyEffectToEffectableAsync(_caster, _bookEffectable, bookTile, turn);
			}
			else if (_bookEffectable != null)
			{
				Vector3 bookPos = _bookEffectable.GetReferenceTransform() != null
					? _bookEffectable.GetReferenceTransform().position
					: Vector3.zero;
				LakiTileEffectApplyDebug.LogUnitOnTile("Clone", turn, bookTile, default, bookPos);
			}

			await Task.Delay(PostEffectsPauseMs);
		}

		public async Task ExecuteRerollPhaseAsync()
		{
			int turn = _turnState != null ? _turnState.TurnNumber : 0;
			Vector3 playerPos = (_nara != null && _nara.NaraViewGO != null) ? _nara.NaraViewGO.transform.position : Vector3.zero;
			int playerTile = _arena.ComputeTileIndex(playerPos, _centerWorld);
			int nextTurn = turn + 1;
			await LakiArenaRerollPresentation.RunShuffleWithTurnoSfxAsync(
				_arena,
				_visual,
				_audioService,
				turn,
				playerTile,
				nextTurn,
				new System.Random(nextTurn * 7919 + 17));
		}

		public void SetCenter(Vector3 centerWorld)
		{
			_centerWorld = centerWorld;
		}
	}
}
