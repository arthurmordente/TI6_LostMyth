using System;
using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;

public sealed class LobbyInteractionZoneView : MonoBehaviour
{
    [SerializeField] private LobbyInteractionKind _kind;
    [SerializeField] private LobbyFHintView _hintView;

    public LobbyInteractionKind Kind => _kind;
    public LobbyFHintView HintView => _hintView;

    public void Configure(LobbyInteractionKind kind, LobbyFHintView hintView)
    {
        _kind = kind;
        _hintView = hintView;
        _hintView?.SetVisible(false);
    }

    private Action<LobbyInteractionZoneView> _onEnter;
    private Action<LobbyInteractionZoneView> _onExit;

    public void Setup(Action<LobbyInteractionZoneView> onEnter, Action<LobbyInteractionZoneView> onExit)
    {
        _onEnter = onEnter;
        _onExit = onExit;
        _hintView?.SetVisible(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<NaraView>(out _))
            return;

        _onEnter?.Invoke(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<NaraView>(out _))
            return;

        _onExit?.Invoke(this);
    }

    void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null)
            return;

        Gizmos.color = _kind == LobbyInteractionKind.TipsCatalog
            ? new Color(0.2f, 0.6f, 1f, 0.35f)
            : new Color(0.2f, 1f, 0.4f, 0.35f);
        var matrix = Matrix4x4.TRS(transform.TransformPoint(box.center), transform.rotation, transform.lossyScale);
        Gizmos.matrix = matrix;
        Gizmos.DrawCube(Vector3.zero, box.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
