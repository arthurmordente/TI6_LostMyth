using UnityEngine;
using TMPro;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice
{
	public class DicePanelsView : MonoBehaviour
	{
		[SerializeField] private GameObject _lakiPanelRoot;
		[SerializeField] private TMP_Text _lakiSumText;
		[SerializeField] private TMP_Text _lakiFactorsText;
		[SerializeField] private GameObject _playerPanelRoot;
		[SerializeField] private TMP_Text _playerSumText;
		[SerializeField] private TMP_Text _playerFactorsText;

		public void SetRefs(GameObject lakiPanelRoot, TMP_Text lakiSumText, TMP_Text lakiFactorsText,
			GameObject playerPanelRoot, TMP_Text playerSumText, TMP_Text playerFactorsText)
		{
			_lakiPanelRoot = lakiPanelRoot;
			_lakiSumText = lakiSumText;
			_lakiFactorsText = lakiFactorsText;
			_playerPanelRoot = playerPanelRoot;
			_playerSumText = playerSumText;
			_playerFactorsText = playerFactorsText;
			EnsureHidden();
		}

		private void OnEnable()
		{
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnProgress += OnProgress;
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnFinalAnimation += OnFinal;
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnReset += OnReset;
			OnReset();
		}

		private void OnDisable()
		{
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnProgress -= OnProgress;
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnFinalAnimation -= OnFinal;
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnReset -= OnReset;
		}

		private void OnReset()
		{
			EnsureHidden();
		}

		private void EnsureHidden()
		{
			if (_lakiPanelRoot != null) _lakiPanelRoot.SetActive(false);
			if (_playerPanelRoot != null) _playerPanelRoot.SetActive(false);
			if (_lakiSumText != null) _lakiSumText.SetText("0");
			if (_playerSumText != null) _playerSumText.SetText("0");
			if (_lakiFactorsText != null) _lakiFactorsText.SetText(string.Empty);
			if (_playerFactorsText != null) _playerFactorsText.SetText(string.Empty);
		}

		private void OnProgress(System.Collections.Generic.List<int> pRolls, int pSum, System.Collections.Generic.List<int> bRolls, int bSum)
		{
			if (_lakiPanelRoot != null) _lakiPanelRoot.SetActive(true);
			if (_playerPanelRoot != null) _playerPanelRoot.SetActive(true);
			if (_playerSumText != null) _playerSumText.SetText(pSum.ToString());
			if (_lakiSumText != null) _lakiSumText.SetText(bSum.ToString());
			if (_playerFactorsText != null) _playerFactorsText.SetText(FormatFactors(pRolls));
			if (_lakiFactorsText != null) _lakiFactorsText.SetText(FormatFactors(bRolls));
		}

		private void OnFinal(int pSum, int bSum)
		{
			if (_playerSumText != null) _playerSumText.SetText(pSum.ToString());
			if (_lakiSumText != null) _lakiSumText.SetText(bSum.ToString());
			// Keep panels visible until Reset is fired at the start of next turn's processing
		}

		private static string FormatFactors(System.Collections.Generic.List<int> rolls)
		{
			if (rolls == null || rolls.Count == 0) return string.Empty;
			if (rolls.Count == 1) return rolls[0].ToString();
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			for (int i = 0; i < rolls.Count; i++)
			{
				if (i > 0) sb.Append(" + ");
				sb.Append(rolls[i]);
			}
			return sb.ToString();
		}
	}
}


