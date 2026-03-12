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

    // An invisible proxy that Cinemachine always follows.
    // We interpolate this proxy's position between targets — unit transforms are never touched.
    private Transform _followProxy;
    private Vector3 _transitionFromPos;
    private float _transitionElapsed = float.MaxValue;

    private void Awake()
    {
        // Create a root-level proxy so its world position is never affected by parent transforms.
        var proxyGO = new GameObject("CameraFollowProxy");
        _followProxy = proxyGO.transform;

        if (_cineCam != null)
        {
            if (_orbital == null) _orbital = _cineCam.GetComponent<CinemachineOrbitalFollow>();
            // Initialise proxy at the Inspector-assigned follow target's position so there
            // is no jump on the first frame.
            if (_cineCam.Follow != null)
            {
                _followProxy.position = _cineCam.Follow.position;
                if (_target == null) _target = _cineCam.Follow;
            }
            // Point Cinemachine at the proxy; we never change this again.
            _cineCam.Follow = _followProxy;
        }
    }

    public void SetNewTarget(Transform target)
    {
        if (_cineCam == null || _followProxy == null) return;
        if (_orbital == null) _orbital = _cineCam.GetComponent<CinemachineOrbitalFollow>();

        if (target != _target)
        {
            // Record where the proxy currently is so the lerp starts from here.
            _transitionFromPos = _followProxy.position;
            _transitionElapsed = 0f;
        }

        _target = target;
        // _cineCam.Follow stays pointed at _followProxy — do not reassign it here.
    }

    public void UpdateCameraRotation(float mouseDeltaX, float deltaTime)
    {
        if (_cineCam == null || _followProxy == null) return;
        if (_orbital == null) _orbital = _cineCam.GetComponent<CinemachineOrbitalFollow>();

        _horizontalAngle += mouseDeltaX * _velocidade * deltaTime;
        _orbital.HorizontalAxis.Value = _horizontalAngle;

        if (_target != null)
        {
            if (_transitionElapsed < _transitionDuration)
            {
                _transitionElapsed += deltaTime;
                float t = Mathf.Clamp01(_transitionElapsed / _transitionDuration);
                _followProxy.position = Vector3.Lerp(_transitionFromPos, _target.position, Mathf.SmoothStep(0f, 1f, t));
            }
            else
            {
                _followProxy.position = _target.position;
            }
        }
    }

    public void SetTargetNull()
    {
        _target = null;
    }

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
            _followProxy.position = _target.position;
    }
}
