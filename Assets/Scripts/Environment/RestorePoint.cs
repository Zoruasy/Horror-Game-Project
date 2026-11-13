using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RestorePoint : Interactable
{
    public override string GetPrompt(GameObject interactor) => "Restor";

    public override void Interact(GameObject interactor)
    {
        SaveSystem.DeleteSave();
        SaveSystem.ResetLevel();
    }
}
