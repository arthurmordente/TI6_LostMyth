public interface ILobbyInteractionZoneController
{
    void Setup(LobbyInteractionZoneView[] zones);
    void Clear();
    LobbyInteractionZoneView GetActiveZone();
}
