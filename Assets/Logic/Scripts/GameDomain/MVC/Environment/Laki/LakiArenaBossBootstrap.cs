using UnityEngine;
using Zenject;
using Logic.Scripts.Turns;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.GameDomain.MVC.Nara;
using TMPro;
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

		[SerializeReference] private System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _positiveEffects;
		[SerializeReference] private System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _negativeEffects;
		[SerializeField] private int _initialPlayerChips = 3;
		[SerializeField] private int _initialBossChips = 3;
		[SerializeField] private Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.ChipUiSkin _chipUiSkin;
		[SerializeField] private float _uiScale = 2f;

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

			if (_chipUiSkin == null)
			{
				try { _chipUiSkin = container.Resolve<Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.ChipUiSkin>(); }
				catch { _chipUiSkin = null; }
			}

			var arenaService = new RouletteArenaService(_innerRadius, _outerRadius, _radialSplit01);
			arenaService.SetEffectPools(_positiveEffects, _negativeEffects);
			var viewGO = new GameObject("LakiRouletteArena");
			var view = viewGO.AddComponent<LakiRouletteArenaView>();
			view.SetGeometry(_centerWorld, _innerRadius, _outerRadius, _radialSplit01);
			view.RefreshFrom(arenaService);
			var casterRelay = GetComponent<Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay>();
			IEffectable caster = casterRelay != null ? casterRelay as IEffectable : null;
			var actor = new LakiRouletteArenaActor(_turnStateService, _naraController, arenaService, _centerWorld, view, caster);
			var cmd = _commandFactory.CreateCommandVoid<Logic.Scripts.GameDomain.Commands.RegisterEnvironmentActorCommand>();
			cmd.SetActor(actor);
			cmd.Execute();

			IChipService chipSvc = null;
			try { chipSvc = container.Resolve<IChipService>(); } catch { chipSvc = null; }
			if (chipSvc != null) chipSvc.SetInitial(_initialPlayerChips, _initialBossChips);
			BuildChipUi(container);
		}

		private static Sprite CreateSquareSprite(Color color)
		{
			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			tex.SetPixel(0, 0, color);
			tex.SetPixel(1, 0, color);
			tex.SetPixel(0, 1, color);
			tex.SetPixel(1, 1, color);
			tex.Apply(false, false);
			return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
		}

		private void BuildChipUi(DiContainer container)
		{
			var uiRoot = new GameObject("LakiChipsUI");
			var canvas = uiRoot.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			var scaler = uiRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
			scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0.5f;
			uiRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
			uiRoot.transform.localScale = new Vector3(_uiScale, _uiScale, 1f);
			UnityEngine.UI.Image playerIcon = null;
			TextMeshProUGUI playerText = null;
			TextMeshProUGUI playerDiceText = null;
			UnityEngine.UI.Image bossIcon = null;
			TextMeshProUGUI bossText = null;
			TextMeshProUGUI bossDiceText = null;
			TextMeshProUGUI potText = null;

			// Resolve prefabs from ChipUiSkin (SO) if provided
			GameObject playerPrefab = _chipUiSkin != null ? _chipUiSkin.PlayerChipUiPrefab : null;
			GameObject bossPrefab = _chipUiSkin != null ? _chipUiSkin.BossChipUiPrefab : null;
			GameObject potPrefab = _chipUiSkin != null ? _chipUiSkin.PotChipUiPrefab : null;
			bool usePrefabs = (playerPrefab != null && bossPrefab != null && potPrefab != null);
			if (usePrefabs)
			{
				// Player
				var playerPanel = Instantiate(playerPrefab);
				playerPanel.name = "PlayerChips";
				playerPanel.transform.SetParent(uiRoot.transform, false);
				var rtPP = playerPanel.GetComponent<RectTransform>() ?? playerPanel.AddComponent<RectTransform>();
				rtPP.anchorMin = new Vector2(0f, 1f);
				rtPP.anchorMax = new Vector2(0f, 1f);
				rtPP.pivot = new Vector2(0f, 1f);
				rtPP.anchoredPosition = new Vector2(20f, -20f);
				var iconNodeP = playerPanel.transform.Find("Icon");
				if (iconNodeP == null) iconNodeP = playerPanel.transform;
				playerIcon = iconNodeP.GetComponent<UnityEngine.UI.Image>() ?? playerPanel.GetComponentInChildren<UnityEngine.UI.Image>(true);
				var textNodeP = iconNodeP.Find("Text");
				if (textNodeP == null) textNodeP = playerPanel.transform.Find("Text");
				if (textNodeP != null) playerText = textNodeP.GetComponent<TextMeshProUGUI>();
				if (playerText == null) playerText = playerPanel.GetComponentInChildren<TextMeshProUGUI>(true);
				var diceNodeP = playerPanel.transform.Find("DiceText");
				if (diceNodeP != null) playerDiceText = diceNodeP.GetComponent<TextMeshProUGUI>();

				// Boss
				var bossPanel = Instantiate(bossPrefab);
				bossPanel.name = "BossChips";
				bossPanel.transform.SetParent(uiRoot.transform, false);
				var rtBP = bossPanel.GetComponent<RectTransform>() ?? bossPanel.AddComponent<RectTransform>();
				rtBP.anchorMin = new Vector2(1f, 1f);
				rtBP.anchorMax = new Vector2(1f, 1f);
				rtBP.pivot = new Vector2(1f, 1f);
				rtBP.anchoredPosition = new Vector2(-20f, -20f);
				var iconNodeB = bossPanel.transform.Find("Icon");
				if (iconNodeB == null) iconNodeB = bossPanel.transform;
				bossIcon = iconNodeB.GetComponent<UnityEngine.UI.Image>() ?? bossPanel.GetComponentInChildren<UnityEngine.UI.Image>(true);
				var textNodeB = iconNodeB.Find("Text");
				if (textNodeB == null) textNodeB = bossPanel.transform.Find("Text");
				if (textNodeB != null) bossText = textNodeB.GetComponent<TextMeshProUGUI>();
				if (bossText == null) bossText = bossPanel.GetComponentInChildren<TextMeshProUGUI>(true);
				var diceNodeB = bossPanel.transform.Find("DiceText");
				if (diceNodeB != null) bossDiceText = diceNodeB.GetComponent<TextMeshProUGUI>();

				// Pot
				var potPanel = Instantiate(potPrefab);
				potPanel.name = "PotChips";
				potPanel.transform.SetParent(uiRoot.transform, false);
				var rtPot = potPanel.GetComponent<RectTransform>() ?? potPanel.AddComponent<RectTransform>();
				rtPot.anchorMin = new Vector2(0.5f, 0.75f);
				rtPot.anchorMax = new Vector2(0.5f, 0.75f);
				rtPot.pivot = new Vector2(0.5f, 0.5f);
				rtPot.anchoredPosition = new Vector2(0f, 0f);
				var iconNodeT = potPanel.transform.Find("Icon");
				if (iconNodeT == null) iconNodeT = potPanel.transform;
				var textNodeT = iconNodeT.Find("Text");
				if (textNodeT == null) textNodeT = potPanel.transform.Find("Text");
				if (textNodeT != null) potText = textNodeT.GetComponent<TextMeshProUGUI>();
				if (potText == null) potText = potPanel.GetComponentInChildren<TextMeshProUGUI>(true);
			}
			else
			{
				// Fallback dynamic build (existing)
				var playerPanel = new GameObject("PlayerChips");
				playerPanel.transform.SetParent(uiRoot.transform, false);
				var rtPP = playerPanel.AddComponent<RectTransform>();
				rtPP.anchorMin = new Vector2(0f, 1f);
				rtPP.anchorMax = new Vector2(0f, 1f);
				rtPP.pivot = new Vector2(0f, 1f);
				rtPP.anchoredPosition = new Vector2(20f, -20f);
				var playerIconGO = new GameObject("Icon");
				playerIconGO.transform.SetParent(playerPanel.transform, false);
				playerIcon = playerIconGO.AddComponent<UnityEngine.UI.Image>();
				var playerChipSprite = _chipUiSkin != null && _chipUiSkin.PlayerChip != null
					? _chipUiSkin.PlayerChip
					: CreateSquareSprite(new Color(0.6f, 0f, 0.8f, 1f));
				playerIcon.sprite = playerChipSprite;
				playerIcon.color = Color.white;
				var rtPI = playerIcon.rectTransform;
				rtPI.anchorMin = new Vector2(0f, 0.5f);
				rtPI.anchorMax = new Vector2(0f, 0.5f);
				rtPI.pivot = new Vector2(0f, 0.5f);
				rtPI.anchoredPosition = new Vector2(0f, 0f);
				rtPI.sizeDelta = new Vector2(48f, 48f);
				var playerTextGO = new GameObject("Text");
				playerTextGO.transform.SetParent(playerIconGO.transform, false);
				playerText = playerTextGO.AddComponent<TextMeshProUGUI>();
				playerText.alignment = TextAlignmentOptions.Center;
				playerText.fontSize = 32f;
				var rtPT = playerText.rectTransform;
				rtPT.anchorMin = new Vector2(0.5f, 0.5f);
				rtPT.anchorMax = new Vector2(0.5f, 0.5f);
				rtPT.pivot = new Vector2(0.5f, 0.5f);
				rtPT.anchoredPosition = new Vector2(0f, 0f);
				rtPT.sizeDelta = new Vector2(48f, 48f);
				var playerDiceTextGO = new GameObject("DiceText");
				playerDiceTextGO.transform.SetParent(playerPanel.transform, false);
				playerDiceText = playerDiceTextGO.AddComponent<TextMeshProUGUI>();
				playerDiceText.alignment = TextAlignmentOptions.Center;
				playerDiceText.textWrappingMode = TextWrappingModes.NoWrap;
				playerDiceText.overflowMode = TextOverflowModes.Ellipsis;
				playerDiceText.fontSize = 20f;
				var rtPDT = playerDiceText.rectTransform;
				rtPDT.anchorMin = new Vector2(0f, 1f);
				rtPDT.anchorMax = new Vector2(0f, 1f);
				rtPDT.pivot = new Vector2(0f, 1f);
				rtPDT.anchoredPosition = new Vector2(24f, -60f);
				rtPDT.sizeDelta = new Vector2(48f, 24f);
				var bossPanel = new GameObject("BossChips");
				bossPanel.transform.SetParent(uiRoot.transform, false);
				var rtBP = bossPanel.AddComponent<RectTransform>();
				rtBP.anchorMin = new Vector2(1f, 1f);
				rtBP.anchorMax = new Vector2(1f, 1f);
				rtBP.pivot = new Vector2(1f, 1f);
				rtBP.anchoredPosition = new Vector2(-20f, -20f);
				var bossIconGO = new GameObject("Icon");
				bossIconGO.transform.SetParent(bossPanel.transform, false);
				bossIcon = bossIconGO.AddComponent<UnityEngine.UI.Image>();
				var bossChipSprite = _chipUiSkin != null && _chipUiSkin.BossChip != null
					? _chipUiSkin.BossChip
					: CreateSquareSprite(new Color(0.9f, 0.1f, 0.1f, 1f));
				bossIcon.sprite = bossChipSprite;
				bossIcon.color = Color.white;
				var rtBI = bossIcon.rectTransform;
				rtBI.anchorMin = new Vector2(1f, 0.5f);
				rtBI.anchorMax = new Vector2(1f, 0.5f);
				rtBI.pivot = new Vector2(1f, 0.5f);
				rtBI.anchoredPosition = new Vector2(0f, 0f);
				rtBI.sizeDelta = new Vector2(48f, 48f);
				var bossTextGO = new GameObject("Text");
				bossTextGO.transform.SetParent(bossIconGO.transform, false);
				bossText = bossTextGO.AddComponent<TextMeshProUGUI>();
				bossText.alignment = TextAlignmentOptions.Center;
				bossText.fontSize = 32f;
				var rtBT = bossText.rectTransform;
				rtBT.anchorMin = new Vector2(0.5f, 0.5f);
				rtBT.anchorMax = new Vector2(0.5f, 0.5f);
				rtBT.pivot = new Vector2(0.5f, 0.5f);
				rtBT.anchoredPosition = new Vector2(0f, 0f);
				rtBT.sizeDelta = new Vector2(48f, 48f);
				var bossDiceTextGO = new GameObject("DiceText");
				bossDiceTextGO.transform.SetParent(bossPanel.transform, false);
				bossDiceText = bossDiceTextGO.AddComponent<TextMeshProUGUI>();
				bossDiceText.alignment = TextAlignmentOptions.Center;
				bossDiceText.textWrappingMode = TextWrappingModes.NoWrap;
				bossDiceText.overflowMode = TextOverflowModes.Ellipsis;
				bossDiceText.fontSize = 20f;
				var rtBDT = bossDiceText.rectTransform;
				rtBDT.anchorMin = new Vector2(1f, 1f);
				rtBDT.anchorMax = new Vector2(1f, 1f);
				rtBDT.pivot = new Vector2(1f, 1f);
				rtBDT.anchoredPosition = new Vector2(-24f, -60f);
				rtBDT.sizeDelta = new Vector2(48f, 24f);
				var potPanel = new GameObject("PotChips");
				potPanel.transform.SetParent(uiRoot.transform, false);
				var rtPot = potPanel.AddComponent<RectTransform>();
				rtPot.anchorMin = new Vector2(0.5f, 0.75f);
				rtPot.anchorMax = new Vector2(0.5f, 0.75f);
				rtPot.pivot = new Vector2(0.5f, 0.5f);
				rtPot.anchoredPosition = new Vector2(0f, 0f);
				var potIconGO = new GameObject("Icon");
				potIconGO.transform.SetParent(potPanel.transform, false);
				var potIcon = potIconGO.AddComponent<UnityEngine.UI.Image>();
				var potChipSprite = _chipUiSkin != null && _chipUiSkin.PotChip != null
					? _chipUiSkin.PotChip
					: CreateSquareSprite(new Color(0.1f, 0.8f, 0.2f, 1f));
				potIcon.sprite = potChipSprite;
				potIcon.color = Color.white;
				var rtPIcon = potIcon.rectTransform;
				rtPIcon.anchorMin = new Vector2(0.5f, 1f);
				rtPIcon.anchorMax = new Vector2(0.5f, 1f);
				rtPIcon.pivot = new Vector2(0.5f, 1f);
				rtPIcon.anchoredPosition = new Vector2(0f, 0f);
				rtPIcon.sizeDelta = new Vector2(48f, 48f);
				var potTextGO = new GameObject("Text");
				potTextGO.transform.SetParent(potIconGO.transform, false);
				potText = potTextGO.AddComponent<TextMeshProUGUI>();
				potText.alignment = TextAlignmentOptions.Center;
				potText.fontSize = 32f;
				var rtPTxt = potText.rectTransform;
				rtPTxt.anchorMin = new Vector2(0.5f, 0.5f);
				rtPTxt.anchorMax = new Vector2(0.5f, 0.5f);
				rtPTxt.pivot = new Vector2(0.5f, 0.5f);
				rtPTxt.anchoredPosition = new Vector2(0f, 0f);
				rtPTxt.sizeDelta = new Vector2(48f, 48f);
			}
			var view = uiRoot.GetComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.ChipUiView>();
			if (view == null) view = uiRoot.AddComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.ChipUiView>();
			if (view == null)
			{
				var uiChild = new GameObject("ChipUIView");
				uiChild.transform.SetParent(uiRoot.transform, false);
				view = uiChild.AddComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.ChipUiView>();
			}
			view?.SetRefs(playerIcon, playerText, bossIcon, bossText, potText, playerDiceText, bossDiceText, 0.05f);
			UnityEngine.Debug.Log($"[Laki] Chips UI created: GO='{uiRoot.name}' viewOnRoot={(uiRoot.GetComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.ChipUiView>()!=null)} viewAssigned={(view!=null)}");
		}
		
		private void OnDestroy()
		{
			try { Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.Reset(); } catch { }
		}
	}
}


