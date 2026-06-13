using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Echo {
    public class EchoFactory {
        private readonly EchoView _echoViewPrefab;

        public EchoFactory(EchoView echoViewPrefab) {
            _echoViewPrefab = echoViewPrefab;
        }

        public EchoView CreateEcho(int castTime, Transform referenceTransform) {
            Debug.LogWarning("Is null refTransform: " + (referenceTransform == null));
            Debug.LogWarning("Is null echoprefab: " + (_echoViewPrefab == null));
            EchoView echo = Object.Instantiate(_echoViewPrefab, referenceTransform.position, referenceTransform.rotation);
			Logic.Scripts.GameDomain.MVC.Environment.Orb.OrbController.RetargetAllTo(echo.transform);
            return echo;
        }
    }
}