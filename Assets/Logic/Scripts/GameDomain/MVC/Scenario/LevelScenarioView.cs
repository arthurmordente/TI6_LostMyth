using UnityEngine;

public class LevelScenarioView : MonoBehaviour {
    [field: SerializeField] public PortalView[] PortalViews { get; private set; }
    [field: SerializeField] public InteractableObjects[] Interactableviews { get; private set; }
    [field: SerializeField] public InteractableEchoObjects[] EchoInteractableview { get; private set; }
    [field: SerializeField] public LobbyInteractionZoneView[] LobbyInteractionZones { get; private set; }
}
