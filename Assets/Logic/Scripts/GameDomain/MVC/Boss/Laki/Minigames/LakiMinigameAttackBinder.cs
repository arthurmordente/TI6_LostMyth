using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames
{
	[DisallowMultipleComponent]
	public class LakiMinigameAttackBinder : MonoBehaviour
	{
		[SerializeField] private GameObject _roundPrefab;
		public GameObject RoundPrefab => _roundPrefab;
	}
}


