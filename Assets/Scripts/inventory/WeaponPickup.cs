using UnityEngine;

public class WeaponPickup : Item
{
    public override string GetPrompt(GameObject interactor) => "pick up dagger to backstab";

    public override bool CanInteract(GameObject interactor)
    {
        Inventory inventory = interactor.GetComponentInParent<Inventory>();
        return inventory && !inventory.Has<WeaponPickup>();
    }
}
