using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	// Attach this to the Laki UI Canvas prefab and wire references in the inspector.
	public class LakiArenaUiBindings : MonoBehaviour
	{
		[Header("Header Panel")]
		public TMP_Text MinigameNameText;
		public TMP_Text PotText;

		[Header("Chips Panels")]
		public Image NaraChipIcon;
		public TMP_Text NaraChipsText;
		public Image LakiChipIcon;
		public TMP_Text LakiChipsText;

		[Header("Dice Panels - Laki")]
		public GameObject LakiDicePanelRoot;
		public Image LakiDiceImage;
		public TMP_Text LakiDiceSumText;
		public TMP_Text LakiDiceFactorsText;

		[Header("Dice Panels - Player")]
		public GameObject PlayerDicePanelRoot;
		public Image PlayerDiceImage;
		public TMP_Text PlayerDiceSumText;
		public TMP_Text PlayerDiceFactorsText;
	}
}


