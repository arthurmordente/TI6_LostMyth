using UnityEngine;
using Zenject;
using TMPro;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	// Attach this to the same object as LakiArenaBossBootstrap.
	// It instantiates the Laki UI canvas prefab and wires existing services/views.
	public class LakiArenaUiBootstrap : MonoBehaviour
	{
		[SerializeField] private GameObject _uiCanvasPrefab;
		[SerializeField] private float _uiScale = 1f;

		private LakiArenaUiBindings _bindings;

		private void Start()
		{
			if (_uiCanvasPrefab == null)
			{
				Debug.LogError("[Laki UI] Missing UI Canvas prefab reference on LakiArenaUiBootstrap.");
				return;
			}

			var canvasInstance = Instantiate(_uiCanvasPrefab);
			canvasInstance.name = "LakiArenaUI";
			canvasInstance.transform.localScale = new Vector3(_uiScale, _uiScale, 1f);

			_bindings = canvasInstance.GetComponent<LakiArenaUiBindings>();
			if (_bindings == null)
			{
				Debug.LogError("[Laki UI] LakiArenaUiBindings component not found on UI canvas prefab instance.");
				return;
			}

			WireChipsAndPot();
			WireDicePanels();
			WireMinigameName();
		}

		private void WireChipsAndPot()
		{
			// Reuse ChipUiView for chips + pot behavior. Dice texts are handled by DicePanelsView separately.
			var view = _bindings.gameObject.GetComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.ChipUiView>();
			if (view == null) view = _bindings.gameObject.AddComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.ChipUiView>();
			view.SetRefs(
				_bindings.NaraChipIcon,
				_bindings.NaraChipsText,
				_bindings.LakiChipIcon,
				_bindings.LakiChipsText,
				_bindings.PotText,
				null,
				null,
				0.05f
			);
		}

		private void WireDicePanels()
		{
			var diceView = _bindings.gameObject.GetComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DicePanelsView>();
			if (diceView == null) diceView = _bindings.gameObject.AddComponent<Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DicePanelsView>();
			diceView.SetRefs(
				_bindings.LakiDicePanelRoot,
				_bindings.LakiDiceSumText,
				_bindings.LakiDiceFactorsText,
				_bindings.PlayerDicePanelRoot,
				_bindings.PlayerDiceSumText,
				_bindings.PlayerDiceFactorsText
			);
		}

		private void WireMinigameName()
		{
			// Subscribe to name changes during minigame lifecycle
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.OnMinigameNameChanged += OnMinigameNameChanged;
			OnMinigameNameChanged(Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.ActiveMinigameName);
		}

		private void OnDestroy()
		{
			try { Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.OnMinigameNameChanged -= OnMinigameNameChanged; } catch { }
		}

		private void OnMinigameNameChanged(string name)
		{
			if (_bindings == null || _bindings.MinigameNameText == null) return;
			_bindings.MinigameNameText.SetText(string.IsNullOrEmpty(name) ? string.Empty : name);
		}
	}
}


