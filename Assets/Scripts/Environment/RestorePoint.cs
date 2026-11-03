using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RestorePoint : Interactable
{
    void Reset() => GetComponent<Collider>().isTrigger = true;

    public override void Interact(GameObject interactor)
    {
        SaveSystem.DeleteSave();
        SaveSystem.ResetLevel();
    }
}
