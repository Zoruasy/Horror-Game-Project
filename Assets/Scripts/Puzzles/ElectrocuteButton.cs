using UnityEngine;

public class ElectrocuteButton : Interactable
{
    [SerializeField] ElectrocutionPuzzleSequence sequence;

    void Start()
    {
        if (sequence == null)
            Debug.LogError($"{nameof(ElectrocuteButton)} on {name} has no puzzle sequence assigned.", this);
    }

    public override string GetPrompt(GameObject interactor)
    {
        return sequence != null && sequence.CanElectrocute
            ? "Press electrocute button"
            : "Power on and solve the blood vials puzzle first";
    }

    public override bool CanInteract(GameObject interactor)
    {
        return sequence != null && sequence.CanElectrocute;
    }

    public override void Interact(GameObject interactor)
    {
        if (sequence != null)
            sequence.CompleteElectrocution();
    }
}
