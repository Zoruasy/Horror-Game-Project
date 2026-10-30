using UnityEngine;

public class Key : Interactable
{
    public override string GetPrompt(GameObject interactor) => "pick up key";

    public override bool CanInteract(GameObject interactor) => interactor.GetComponentInParent<Inventory>();

    public override void Interact(GameObject interactor)
    {
        Inventory inventory = interactor.GetComponentInParent<Inventory>();
        if (!inventory) return;

        inventory.Add(this);
        gameObject.SetActive(false);
    }
}
