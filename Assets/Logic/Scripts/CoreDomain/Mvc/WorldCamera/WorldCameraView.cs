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

    public void SetNewTarget(Transform target)
    {
        if (_cineCam == null) return;
        if (_orbital == null) _orbital = _cineCam.GetComponent<CinemachineOrbitalFollow>();
        _target = target;
        _cineCam.Follow = _target;
    }

    public void UpdateCameraRotation(float mouseDeltaX, float deltaTime)
    {
        if (_cineCam == null) return;
        if (_orbital == null) _orbital = _cineCam.GetComponent<CinemachineOrbitalFollow>();

        _horizontalAngle += mouseDeltaX * _velocidade * deltaTime;
        _orbital.HorizontalAxis.Value = _horizontalAngle;

        if (_target != null)
            _orbital.FollowTarget.position = _target.position;
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
        float newH = Mathf.Clamp(settings.Center.Height + delta, _minHeight, _maxHeight);
        float newR = Mathf.Clamp(settings.Center.Radius + delta, _minRadius, _maxRadius);

        settings.Center.Height = newH;
        settings.Center.Radius = newR;
        _orbital.Orbits = settings;

        if (_target != null)
            _orbital.FollowTarget.position = _target.position;
    }
}
