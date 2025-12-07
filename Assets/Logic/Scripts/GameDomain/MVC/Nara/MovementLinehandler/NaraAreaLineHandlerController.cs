using Logic.Scripts.Services.UpdateService;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara {
    public class NaraAreaLineHandlerController : IUpdatable, System.IDisposable {
        private readonly NaraAreaLineHandlerView _lineHandlerViewPrefab;
        private readonly int _segments;
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private NaraAreaLineHandlerView _lineHandlerView;
        private Vector3 _center;
        private float _maxRadius;
        private float _radius;
        private Transform _referenceTransform;
        private bool _listening;

        public NaraAreaLineHandlerController(NaraConfigurationSO naraConfiguration,
            IUpdateSubscriptionService updateSubscriptionService, int segments = 100) {
            _segments = Mathf.Max(8, segments);
            _maxRadius = naraConfiguration.MaxMovementRadius;
            _lineHandlerViewPrefab = naraConfiguration.NaraAreaLineHandlerView;
            _updateSubscriptionService = updateSubscriptionService;
        }

        public void InitEntryPoint(Transform referenceTransform) {
            _referenceTransform = referenceTransform;
            _lineHandlerView = Object.Instantiate(_lineHandlerViewPrefab, _referenceTransform).GetComponent<NaraAreaLineHandlerView>();
            Refresh(_center, _maxRadius, _referenceTransform.position);
            SetVisible(false);
        }

        public void RegisterListeners() {
            _updateSubscriptionService.RegisterUpdatable(this);
            _listening = true;
        }

        public void UnregisterListeners() {
            if (_listening) {
                try { _updateSubscriptionService.UnregisterUpdatable(this); } catch { }
                _listening = false;
            }
        }

        public void Refresh(Vector3 center, float radius, Vector3 playerPos) {
            SetFullMoveArea(center, radius);
            SetResultMoveArea(playerPos);
        }

        public void SetFullMoveArea(Vector3 center, float radius) {
            _center = center;
            _radius = Mathf.Max(0f, radius);
            DrawCircle(_lineHandlerView.FullMoveArea, center, _radius, center.y + 0.25f);
        }

        public void SetResultMoveArea(Vector3 playerPos) {
            float dist = Vector3.Distance(playerPos, _center);
            float r2 = Mathf.Max(0f, _radius - dist);
            if (r2 <= 0f) {
                _lineHandlerView.ResultMoveArea.positionCount = 0;
                return;
            }
            DrawCircle(_lineHandlerView.ResultMoveArea, playerPos, r2, _center.y + 0.25f);
        }

        public void SetVisible(bool visible) {
            _lineHandlerView.FullMoveArea.enabled = visible;
            _lineHandlerView.ResultMoveArea.enabled = visible;
            if (visible) {
                RegisterListeners();
            }
            else {
                UnregisterListeners();
            }
        }

        private void DrawCircle(LineRenderer lr, Vector3 center, float radius, float y) {
            int perCircle = _segments + 1;
            int total = perCircle;
            lr.positionCount = total;
            for (int i = 0; i <= _segments; i++) {
                float t = (float)i / _segments;
                float ang = t * 2f * Mathf.PI;
                float x = Mathf.Cos(ang) * radius + center.x;
                float z = Mathf.Sin(ang) * radius + center.z;
                lr.SetPosition(i, new Vector3(x, y, z));
            }
        }

        public void ManagedUpdate() {
            if (_referenceTransform == null || _lineHandlerView == null) return;
            SetResultMoveArea(_referenceTransform.position);
        }

        public void Dispose() {
            UnregisterListeners();
        }
    }
}
