using UnityEngine;
using Zenject;
using Logic.Scripts.Turns;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	public class LakiArenaBossBootstrap : MonoBehaviour
	{
		private TurnStateService _turnStateService;
		private INaraController _naraController;
		private ICommandFactory _commandFactory;

		[SerializeField] private Vector3 _centerWorld = new Vector3(0f, 7f, 0f);
		[SerializeField] private float _innerRadius = RouletteArenaService.INNER_RADIUS_DEFAULT;
		[SerializeField] private float _outerRadius = RouletteArenaService.OUTER_RADIUS_DEFAULT;
		[SerializeField, Range(0f, 1f)] private float _radialSplit01 = 0.6f;
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
			if (container == null) { Debug.LogError("[LakiArenaBossBootstrap] No Zenject container found in this scene."); return; }

			try { _turnStateService = container.Resolve<TurnStateService>(); }
			catch { Debug.LogError("[LakiArenaBossBootstrap] TurnStateService not bound."); return; }
			try { _naraController = container.Resolve<INaraController>(); }
			catch { Debug.LogError("[LakiArenaBossBootstrap] INaraController not bound."); return; }
			try { _commandFactory = container.Resolve<ICommandFactory>(); }
			catch { Debug.LogError("[LakiArenaBossBootstrap] ICommandFactory not bound."); return; }

			// Set arena Y from BossConfiguration.InitialPlayerPosition.y (try multiple sources)
			bool ySet = false;
			try {
				var bossCfg = container.Resolve<Logic.Scripts.GameDomain.MVC.Boss.BossConfigurationSO>();
				_centerWorld = new Vector3(_centerWorld.x, bossCfg.InitialPlayerPosition.y, _centerWorld.z);
				ySet = true;
			} catch { }
			if (!ySet) {
				try {
					var levelTurnData = container.Resolve<LevelTurnData>();
					if (levelTurnData != null && levelTurnData.BossConfiguration != null) {
						float y = levelTurnData.BossConfiguration.InitialPlayerPosition.y;
						_centerWorld = new Vector3(_centerWorld.x, y, _centerWorld.z);
						ySet = true;
					}
				} catch { }
			}

			var arenaService = new RouletteArenaService(_innerRadius, _outerRadius, _radialSplit01, _arcStartDeg, _arcDeg);
			arenaService.SetLayoutConfigs(
				_positiveTileConfig, _neutralTileConfig, _negativeTileConfig,
				_largePositiveEffects, _smallPositiveEffects,
				_largeNegativeEffects, _smallNegativeEffects);
			// Initial roll so the canvas already shows effects when the scene loads
			arenaService.RerollTiles(0, new System.Random(17));
			var viewGO = new GameObject("LakiRouletteArena");
			var view = viewGO.AddComponent<LakiRouletteArenaView>();
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

			IChipService chipSvc = null;
			try { chipSvc = container.Resolve<IChipService>(); } catch { chipSvc = null; }
			if (chipSvc != null) chipSvc.SetInitial(_initialPlayerChips, _initialBossChips);
			// Legacy chips / pot / minigame HUD removed — use DiceAttack prompt prefab on BossAttack when needed.
		}

		private void OnDestroy()
		{
			try { Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.Reset(); } catch { }
			try { Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.Reset(); } catch { }
		}
	}
}


