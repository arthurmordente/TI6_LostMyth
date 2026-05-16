using UnityEngine;
using Zenject;
using Logic.Scripts.Turns;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips;
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using Logic.Scripts.GameDomain.MVC.Environment;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	[ExecuteAlways]
	public class LakiArenaBossBootstrap : MonoBehaviour
	{
		[Header("Debug gizmos (temporary — tuning)")]
		[SerializeField] private bool _drawArenaBoundsGizmo = true;

		private TurnStateService _turnStateService;
		private INaraController _naraController;
		private ICommandFactory _commandFactory;

		[SerializeField] private Vector3 _centerWorld = new Vector3(0f, 0.5f, -4f);
		[SerializeField] private float _innerRadius = RouletteArenaService.INNER_RADIUS_DEFAULT;
		[SerializeField] private float _outerRadius = RouletteArenaService.OUTER_RADIUS_DEFAULT;
		[SerializeField, Range(0f, 1f), Tooltip("Unused. Split uses TILE_RADIAL_DEPTH + 2.5% outer gap (see RouletteArenaService).")]
		private float _radialSplit01 = 0.6f;
		[SerializeField] private float _arcStartDeg = 180f;
		[SerializeField] private float _arcDeg = 180f;

		[Header("Tile Effect Pools")]
		[SerializeReference] private System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _largePositiveEffects;
		[SerializeReference] private System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _smallPositiveEffects;
		[SerializeReference] private System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _largeNegativeEffects;
		[SerializeReference] private System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _smallNegativeEffects;

		[Header("Tile Layout Configs")]
		[Tooltip("Weighted layouts for GREEN (positive) tiles.")]
		[SerializeField] private TileTypeLayoutConfig _positiveTileConfig;
		[Tooltip("Weighted layouts for GREY (neutral) tiles.")]
		[SerializeField] private TileTypeLayoutConfig _neutralTileConfig;
		[Tooltip("Weighted layouts for RED (negative) tiles.")]
		[SerializeField] private TileTypeLayoutConfig _negativeTileConfig;

		[Header("Chips (service only — no HUD)")]
		[SerializeField] private int _initialPlayerChips = 3;
		[SerializeField] private int _initialBossChips = 3;

		[Header("Arena visuals")]
		[Tooltip("Optional override for tile prefabs. When null, uses CombatAttackVisualCatalogSO from the scene Zenject container (same binding as GamePlayInstaller).")]
		[SerializeField] private CombatAttackVisualCatalogSO _combatAttackVisualCatalog;

		[Header("Laki boss shield (prefab child)")]
		[Tooltip("VFX root on the Laki boss prefab. While active: boss is immune and new skill system aim/fresnel on her is suppressed. Disabled on fight turns T and T+1 after the boss loses dice on turn T.")]
		[SerializeField] private GameObject _lakiShieldVfxRoot;

		private int _lastSyncedFightTurn = int.MinValue;

		private const int FightBoardIntroNeutralHoldMs = 2000;

		private void Awake()
		{
			LakiRouletteArenaFightIntro.EnsurePendingIntro();
		}

		private void Start()
		{
			Zenject.DiContainer container = null;
			var sceneCtxs = Object.FindObjectsByType<Zenject.SceneContext>(FindObjectsSortMode.None);
			for (int i = 0; i < sceneCtxs.Length; i++)
			{
				var sc = sceneCtxs[i];
				if (sc != null && sc.gameObject.scene == gameObject.scene)
				{
					container = sc.Container;
					break;
				}
			}
			if (container == null) { Debug.LogError("[LakiArenaBossBootstrap] No Zenject container found in this scene."); LakiRouletteArenaFightIntro.SignalBoardIntroComplete(); return; }

			try { _turnStateService = container.Resolve<TurnStateService>(); }
			catch { Debug.LogError("[LakiArenaBossBootstrap] TurnStateService not bound."); LakiRouletteArenaFightIntro.SignalBoardIntroComplete(); return; }
			try { _naraController = container.Resolve<INaraController>(); }
			catch { Debug.LogError("[LakiArenaBossBootstrap] INaraController not bound."); LakiRouletteArenaFightIntro.SignalBoardIntroComplete(); return; }
			try { _commandFactory = container.Resolve<ICommandFactory>(); }
			catch { Debug.LogError("[LakiArenaBossBootstrap] ICommandFactory not bound."); LakiRouletteArenaFightIntro.SignalBoardIntroComplete(); return; }

			CombatArenaBoundaryRuntime.RegisterLaki(new CombatArenaLakiGeometry
			{
				CenterWorld = _centerWorld,
				InnerRadius = _innerRadius,
				OuterRadius = _outerRadius,
				ArcStartDeg = _arcStartDeg,
				ArcDeg = _arcDeg,
			});

			var arenaService = new RouletteArenaService(_innerRadius, _outerRadius, _radialSplit01, _arcStartDeg, _arcDeg);
			arenaService.SetLayoutConfigs(
				_positiveTileConfig, _neutralTileConfig, _negativeTileConfig,
				_largePositiveEffects, _smallPositiveEffects,
				_largeNegativeEffects, _smallNegativeEffects);
			arenaService.ResetTilesBlank();
			var viewGO = new GameObject("LakiRouletteArena");
			var view = viewGO.AddComponent<LakiRouletteArenaView>();
			var catalog = _combatAttackVisualCatalog;
			if (catalog == null)
			{
				try { catalog = container.Resolve<CombatAttackVisualCatalogSO>(); } catch { catalog = null; }
			}
#if UNITY_EDITOR
			if (catalog == null)
			{
				catalog = AssetDatabase.LoadAssetAtPath<CombatAttackVisualCatalogSO>(
					"Assets/Logic/Scripts/GameDomain/MVC/Boss/Visuals/CombatAttackVisualCatalog.asset");
			}
#endif
			view.SetAttackVisualCatalog(catalog);
			view.SetGeometry(_centerWorld, _innerRadius, _outerRadius, _radialSplit01, _arcStartDeg, _arcDeg);
			view.RefreshFrom(arenaService);
			var casterRelay = GetComponent<Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay>();
			IEffectable caster = casterRelay != null ? casterRelay as IEffectable : null;

			// Resolve the Book as IEffectable so the arena can apply tile effects to it
			IEffectable bookEffectable = null;
			try
			{
				var bookCtrl = container.Resolve<Logic.Scripts.GameDomain.MVC.Book.IBookController>();
				bookEffectable = bookCtrl as IEffectable;
			}
			catch { Debug.LogWarning("[LakiArenaBossBootstrap] IBookController não encontrado no container – Livro não receberá efeitos de casa."); }

			var actor = new LakiRouletteArenaActor(_turnStateService, _naraController, arenaService, _centerWorld, view, caster, bookEffectable);
			var cmd = _commandFactory.CreateCommandVoid<Logic.Scripts.GameDomain.Commands.RegisterEnvironmentActorCommand>();
			cmd.SetActor(actor);
			cmd.Execute();

			_ = RunFightBoardIntroThenReleaseTurnFlowAsync(arenaService, view);

			IChipService chipSvc = null;
			try { chipSvc = container.Resolve<IChipService>(); } catch { chipSvc = null; }
			if (chipSvc != null) chipSvc.SetInitial(_initialPlayerChips, _initialBossChips);
			// Legacy chips / pot / minigame HUD removed — use DiceAttack prompt prefab on BossAttack when needed.

			LakiBossShieldRuntime.RegisterShieldRoot(_lakiShieldVfxRoot);
			if (_turnStateService != null && _turnStateService.Active)
			{
				_lastSyncedFightTurn = _turnStateService.TurnNumber;
				LakiBossShieldRuntime.SyncFightTurn(_turnStateService.TurnNumber);
			}
		}

		private async Task RunFightBoardIntroThenReleaseTurnFlowAsync(RouletteArenaService arenaService, LakiRouletteArenaView view)
		{
			try
			{
				await Task.Delay(FightBoardIntroNeutralHoldMs);
				Vector3 playerPos = (_naraController != null && _naraController.NaraViewGO != null)
					? _naraController.NaraViewGO.transform.position
					: Vector3.zero;
				int playerTile = arenaService.ComputeTileIndex(playerPos, _centerWorld);
				const int turnForIntroSeed = 0;
				for (int i = 0; i < 3; i++)
				{
					arenaService.RandomizeVisualMapping(new System.Random((turnForIntroSeed + i + 1) * 104729 + playerTile));
					view.RefreshFrom(arenaService);
					await Task.Delay(150);
				}
				arenaService.RerollTiles(0, new System.Random(17));
				view.RefreshFrom(arenaService);
			}
			finally
			{
				LakiRouletteArenaFightIntro.SignalBoardIntroComplete();
			}
		}

		private void Update()
		{
			if (_turnStateService == null || !_turnStateService.Active) return;
			int t = _turnStateService.TurnNumber;
			if (t == _lastSyncedFightTurn) return;
			_lastSyncedFightTurn = t;
			LakiBossShieldRuntime.SyncFightTurn(t);
		}

		private void OnDestroy()
		{
			CombatArenaBoundaryRuntime.Clear();
			LakiRouletteArenaFightIntro.CancelWait();
			LakiBossShieldRuntime.Reset();
			try { Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.Reset(); } catch { }
			try { Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.Reset(); } catch { }
		}

		private void OnDrawGizmosSelected()
		{
			if (!_drawArenaBoundsGizmo) return;
			DrawLakiArenaGizmo();
		}

		private void OnDrawGizmos()
		{
			if (!_drawArenaBoundsGizmo) return;
			DrawLakiArenaGizmo();
		}

		private void OnValidate()
		{
#if UNITY_EDITOR
			if (_drawArenaBoundsGizmo)
				UnityEditor.SceneView.RepaintAll();
#endif
		}

		private void DrawLakiArenaGizmo()
		{
			Vector3 center = _centerWorld;
			if (center == Vector3.zero)
				center = transform.position;
			CombatArenaBoundaryGizmoDrawer.DrawLaki(
				center,
				_innerRadius,
				_outerRadius,
				_arcStartDeg,
				_arcDeg);
		}
	}
}


