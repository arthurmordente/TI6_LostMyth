using System;
using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Turns;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Core.Mvc.WorldCamera;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	public sealed class LakiRouletteArenaActor : IEnvironmentTurnActor, ILakiRouletteArenaTurnPhases
	{
		const int PostEffectsPauseMs = 500;
		const float TileApplyCameraBlendSec = 0.45f;

		private readonly ITurnStateReader _turnState;
		private readonly INaraController _nara;
		private readonly RouletteArenaService _arena;
		private readonly IEffectable _caster;
		private readonly IRouletteArenaVisual _visual;
		private readonly IEffectable _bookEffectable;
		private readonly IAudioService _audioService;
		private readonly ICameraFocusService _cameraFocus;
		private Vector3 _centerWorld;
		private CameraFocusHandle _cameraFocusHandle;

		public bool RemoveAfterRun => false;

		public LakiRouletteArenaActor(
			ITurnStateReader turnState,
			INaraController nara,
			RouletteArenaService arena,
			Vector3? centerWorld = null,
			IRouletteArenaVisual visual = null,
			IEffectable caster = null,
			IEffectable bookEffectable = null,
			IAudioService audioService = null,
			ICameraFocusService cameraFocus = null)
		{
			_turnState = turnState;
			_nara = nara;
			_arena = arena ?? new RouletteArenaService();
			_visual = visual;
			_caster = caster;
			_bookEffectable = bookEffectable;
			_audioService = audioService;
			_cameraFocus = cameraFocus;
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
				const int emphasisDurationMs = 500;
				const int emphasisSteps = 10;
				int stepDelayMs = emphasisDurationMs / emphasisSteps;
				for (int i = 0; i <= emphasisSteps; i++)
				{
					float t = (float)i / emphasisSteps;
					_visual.SetEmphasis(tilesToEmphasize, t, 0.85f);
					if (i < emphasisSteps)
						await Task.Delay(stepDelayMs);
				}
			}

			bool cloneApplied = false;
			bool playerWillApply = playerTile >= 0;

			if (bookTile >= 0 && _bookEffectable != null)
			{
				Vector3 bookPos = _bookEffectable.GetReferenceTransform() != null
					? _bookEffectable.GetReferenceTransform().position
					: Vector3.zero;
				LakiTileEffectApplyDebug.LogUnitOnTile(
					"Clone", turn, bookTile, _arena.GetTileEffect(bookTile), bookPos);
				await FocusCameraOnAndWaitBlendAsync(_bookEffectable.GetReferenceTransform());
				await _arena.ApplyEffectToEffectableAsync(_caster, _bookEffectable, bookTile, turn);
				cloneApplied = true;
			}
			else if (_bookEffectable != null)
			{
				Vector3 bookPos = _bookEffectable.GetReferenceTransform() != null
					? _bookEffectable.GetReferenceTransform().position
					: Vector3.zero;
				LakiTileEffectApplyDebug.LogUnitOnTile("Clone", turn, bookTile, default, bookPos);
			}

			if (cloneApplied && playerWillApply)
				await Task.Delay(RouletteArenaService.TileEffectStaggerMs);

			if (playerWillApply)
			{
				LakiTileEffectApplyDebug.LogUnitOnTile(
					"Player", turn, playerTile, _arena.GetTileEffect(playerTile), playerPos);
				Transform playerTransform = _nara != null ? _nara.NaraViewGO?.transform : null;
				await FocusCameraOnAndWaitBlendAsync(playerTransform);
				await _arena.ApplyEffectToPlayerAsync(_caster, _nara, playerTile, turn);
			}
			else
			{
				LakiTileEffectApplyDebug.LogUnitOnTile("Player", turn, playerTile, default, playerPos);
			}

			RestoreCameraAfterTileApply();
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

		async Task FocusCameraOnAndWaitBlendAsync(Transform target)
		{
			ReleaseCameraHandle();
			if (_cameraFocus == null || target == null) return;
			_cameraFocusHandle = _cameraFocus.Follow(target, CameraFocusOptions.Cinematic(TileApplyCameraBlendSec));
			int blendMs = Mathf.RoundToInt(TileApplyCameraBlendSec * 1000f);
			if (blendMs > 0)
				await Task.Delay(blendMs);
		}

		void RestoreCameraAfterTileApply()
		{
			ReleaseCameraHandle();
			_cameraFocus?.RestoreDefaultFollow();
		}

		void ReleaseCameraHandle()
		{
			if (!_cameraFocusHandle.IsValid) return;
			_cameraFocus?.Release(_cameraFocusHandle);
			_cameraFocusHandle = CameraFocusHandle.Invalid;
		}
	}
}
