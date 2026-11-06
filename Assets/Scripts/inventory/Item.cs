using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public abstract class Item : Interactable, ISaveable<Item.State>
{
    bool pickedUp;

    public string SaveId => TryGetComponent(out SaveableEntity entity) ? entity.Id : null;

    public override string GetPrompt(GameObject interactor) => "pick up";

    public override bool CanInteract(GameObject interactor) => interactor.GetComponentInParent<Inventory>();

    public override void Interact(GameObject interactor)
    {
        Inventory inventory = interactor.GetComponentInParent<Inventory>();
        if (!inventory) return;

        inventory.Add(this);
        pickedUp = true;
        gameObject.SetActive(false);
    }


    // ---- ISaveable ----

    public class State
    {
        public bool pickedUp;
    }

    // public int StateVersion => 2;

    // public JToken Migrate(JToken data, int fromVersion)
    // {
    //     if (fromVersion == 1)
    //     {
    //         bool pickedUp = data.Value<bool>("pickedUp");
    //         data["status"] = pickedUp ? "Held" : "InWorld";
    //         ((JObject)data).Remove("pickedUp");
    //     }
    //     return data;
    // }

    public State CaptureState() => new State { pickedUp = pickedUp };

    public void RestoreState(State state)
    {
        pickedUp = state.pickedUp;
        if (pickedUp) gameObject.SetActive(false);
    }
}
