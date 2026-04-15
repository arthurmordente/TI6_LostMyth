using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    /// <summary>
    /// Optional root script on the player-roll prompt prefab. Assign your Canvas layout under this object.
    /// </summary>
    public class DiceAttackPlayerRollPrompt : MonoBehaviour
    {
        [SerializeField] private GameObject _contentRoot;

        private void Awake()
        {
            if (_contentRoot == null) _contentRoot = gameObject;
        }

        public void Show()
        {
            if (_contentRoot != null) _contentRoot.SetActive(true);
        }

        public void Hide()
        {
            if (_contentRoot != null) _contentRoot.SetActive(false);
        }
    }
}
