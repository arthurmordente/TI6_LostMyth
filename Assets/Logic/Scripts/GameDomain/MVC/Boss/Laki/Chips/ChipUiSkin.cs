using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips
{
	[CreateAssetMenu(menuName = "Laki/Chip UI Skin", fileName = "LakiChipUiSkin")]
	public class ChipUiSkin : ScriptableObject
	{
		public Sprite PlayerChip;
		public Sprite BossChip;
		public Sprite PotChip;
	}
}

