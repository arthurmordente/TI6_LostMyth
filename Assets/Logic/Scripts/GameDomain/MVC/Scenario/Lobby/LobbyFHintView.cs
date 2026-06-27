using UnityEngine;

public sealed class LobbyFHintView : MonoBehaviour
{
    [SerializeField] private GameObject _root;

    public void SetVisible(bool visible)
    {
        var target = _root != null ? _root : gameObject;
        if (target.activeSelf != visible)
            target.SetActive(visible);
    }

    void Awake()
    {
        SetVisible(false);
    }
}
