using UnityEngine;
using CharacterController;

public class WeaponPickup : Interactable
{
    public override string GetPrompt(GameObject interactor) => "pick up dagger to backstab";

    public override bool CanInteract(GameObject interactor)
    {
        FirstPersonController player = interactor.GetComponentInParent<FirstPersonController>();
        return player != null && !player.hasWeapon;
    }

    public override void Interact(GameObject interactor)
    {
        FirstPersonController player = interactor.GetComponentInParent<FirstPersonController>();
        if (player == null) return;

        player.PickUpWeapon();
        gameObject.SetActive(false);
    }
}