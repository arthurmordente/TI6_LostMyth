using System.Collections.Generic;
using Logic.Scripts.Extensions;

public sealed class LobbyInteractionZoneController : ILobbyInteractionZoneController
{
    readonly List<LobbyInteractionZoneView> _occupiedZones = new();
    LobbyInteractionZoneView[] _zones;

    public void Setup(LobbyInteractionZoneView[] zones)
    {
        Clear();
        _zones = zones;
        if (_zones.IsNullOrEmpty())
            return;

        foreach (var zone in _zones)
        {
            if (zone == null)
                continue;

            zone.Setup(OnZoneEnter, OnZoneExit);
        }
    }

    public void Clear()
    {
        foreach (var zone in _occupiedZones)
            zone?.HintView?.SetVisible(false);

        _occupiedZones.Clear();

        if (_zones == null)
            return;

        foreach (var zone in _zones)
        {
            if (zone == null)
                continue;

            zone.Setup(null, null);
            zone.HintView?.SetVisible(false);
        }

        _zones = null;
    }

    public LobbyInteractionZoneView GetActiveZone()
    {
        if (_occupiedZones.Count == 0)
            return null;

        return _occupiedZones[_occupiedZones.Count - 1];
    }

    void OnZoneEnter(LobbyInteractionZoneView zone)
    {
        if (zone == null)
            return;

        if (!_occupiedZones.Contains(zone))
            _occupiedZones.Add(zone);

        RefreshHintVisibility();
    }

    void OnZoneExit(LobbyInteractionZoneView zone)
    {
        if (zone == null)
            return;

        _occupiedZones.Remove(zone);
        zone.HintView?.SetVisible(false);
        RefreshHintVisibility();
    }

    void RefreshHintVisibility()
    {
        if (_zones == null)
            return;

        foreach (var zone in _zones)
            zone?.HintView?.SetVisible(false);

        var active = GetActiveZone();
        active?.HintView?.SetVisible(true);
    }
}
