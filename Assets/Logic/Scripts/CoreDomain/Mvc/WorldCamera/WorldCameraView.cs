using UnityEngine;
using Unity.Cinemachine;

public class WorldCameraView : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _cineCam;
    [SerializeField] private Transform _target;
    [SerializeField] private float _velocidade = 50f;

    private float _horizontalAngle = 0f;
    private CinemachineOrbitalFollow _orbital;

    [SerializeField] private float _minHeight = 5f;
    [SerializeField] private float _maxHeight = 14f;
    [SerializeField] private float _minRadius = 2.5f;
    [SerializeField] private float _maxRadius = 11.5f;

    [Header("Target Transition")]
    [SerializeField] private float _transitionDuration = 0.4f;

    [Header("Pan")]
    [SerializeField] private float _panSensitivity = 0.02f;

    Transform _followProxy;
    Vector3 _transitionFromPos;
    float _transitionElapsed = float.MaxValue;
    Vector3 _panOffsetWorld;
    float _panTweenElapsed = float.MaxValue;
    float _panTweenDuration;
    Vector3 _panTweenFrom;

    void Awake()
    {
        var proxyGO = new GameObject("CameraFollowProxy");
        _followProxy = proxyGO.transform;

        if (_cineCam != null)
        {
            if (_orbital == null) _orbital = _cineCam.GetComponent<CinemachineOrbitalFollow>();
            if (_cineCam.Follow != null)
            {
                _followProxy.position = _cineCam.Follow.position;
                if (_target == null) _target = _cineCam.Follow;
            }

            _cineCam.Follow = _followProxy;
        }
    }

    public void SetFollowBlendDuration(float durationSeconds) =>
        _transitionDuration = Mathf.Max(0f, durationSeconds);

    public void SetNewTarget(Transform target)
    {
        if (_cineCam == null || _followProxy == null) return;
        if (_orbital == null) _orbital = _cineCam.GetComponent<CinemachineOrbitalFollow>();

        if (target != _target)
        {
            _transitionFromPos = _followProxy.position - _panOffsetWorld;
            _transitionElapsed = 0f;
        }

        _target = target;
    }

    public void UpdateCameraRotation(float mouseDeltaX, float deltaTime)
    {
        if (_cineCam == null || _followProxy == null) return;
        if (_orbital == null) _orbital = _cineCam.GetComponent<CinemachineOrbitalFollow>();

        _horizontalAngle += mouseDeltaX * _velocidade * deltaTime;
        _orbital.HorizontalAxis.Value = _horizontalAngle;

        UpdatePanTween(deltaTime);

        Vector3 basePos = _followProxy.position - _panOffsetWorld;
        if (_target != null)
        {
            if (_transitionElapsed < _transitionDuration)
            {
                _transitionElapsed += deltaTime;
                float t = Mathf.Clamp01(_transitionElapsed / _transitionDuration);
                basePos = Vector3.Lerp(_transitionFromPos, _target.position, Mathf.SmoothStep(0f, 1f, t));
            }
            else
            {
                basePos = _target.position;
            }
        }

        _followProxy.position = basePos + _panOffsetWorld;
    }

    public void ApplyPanDelta(Vector2 screenDelta)
    {
        _panTweenElapsed = float.MaxValue;

        Camera cam = ResolveOutputCamera();
        if (cam == null) return;

        Vector3 right = cam.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude > 0.0001f) right.Normalize();

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f) forward.Normalize();

        _panOffsetWorld -= (right * screenDelta.x + forward * screenDelta.y) * _panSensitivity;
    }

    public void TweenPanOffsetToZero(float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            _panOffsetWorld = Vector3.zero;
            _panTweenElapsed = float.MaxValue;
            return;
        }

        _panTweenFrom = _panOffsetWorld;
        _panTweenDuration = durationSeconds;
        _panTweenElapsed = 0f;
    }

    void UpdatePanTween(float deltaTime)
    {
        if (_panTweenElapsed >= _panTweenDuration) return;

        _panTweenElapsed += deltaTime;
        float t = Mathf.Clamp01(_panTweenElapsed / _panTweenDuration);
        _panOffsetWorld = Vector3.Lerp(_panTweenFrom, Vector3.zero, Mathf.SmoothStep(0f, 1f, t));
    }

    Camera ResolveOutputCamera()
    {
        if (_cineCam != null)
        {
            Camera cam = _cineCam.GetComponent<Camera>();
            if (cam != null) return cam;
        }

        return Camera.main;
    }

    public void SetTargetNull() => _target = null;

    public void AdjustZoom(float delta)
    {
        if (_cineCam == null) return;
        if (_orbital == null) _orbital = _cineCam.GetComponent<CinemachineOrbitalFollow>();
        if (_orbital == null) return;

        _orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;

        var settings = _orbital.Orbits;
        settings.Center.Height = Mathf.Clamp(settings.Center.Height + delta, _minHeight, _maxHeight);
        settings.Center.Radius = Mathf.Clamp(settings.Center.Radius + delta, _minRadius, _maxRadius);
        _orbital.Orbits = settings;

        if (_target != null && _followProxy != null)
            _followProxy.position = _target.position + _panOffsetWorld;
    }
}
