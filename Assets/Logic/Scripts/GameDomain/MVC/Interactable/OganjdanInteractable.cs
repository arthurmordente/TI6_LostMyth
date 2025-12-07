using UnityEngine;

public class OganjdanInteractable : InteractableObjects {
    public override void OnInteract() {
        Debug.LogWarning("Oganjdan Interact");
        CommandFactory.CreateCommandVoid<OnCustomizeInteractionCommand>().Execute();
    }
}
