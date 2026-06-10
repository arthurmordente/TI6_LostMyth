using Logic.Scripts.GameDomain.MVC.Boss.Laki;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara
{
    [DisallowMultipleComponent]
    public sealed class ErzaMinigameAnimationListener : MonoBehaviour
    {
        [SerializeField] private NaraView _naraView;

        private void Awake()
        {
            if (_naraView == null)
                _naraView = GetComponent<NaraView>();
        }

        private void OnEnable()
        {
            LakiArenaPresentationEvents.OnBetResolved += OnBetResolved;
        }

        private void OnDisable()
        {
            LakiArenaPresentationEvents.OnBetResolved -= OnBetResolved;
        }

        private void OnBetResolved(bool playerWon)
        {
            _naraView?.PlayBetReaction(playerWon);
        }
    }
}
