using UnityEngine;

public class CodeLock : Interactable
{
    [Tooltip("Leeg laten = deur wordt automatisch gezocht in de parents")]
    [SerializeField] Interactable doorOverride;
    [SerializeField] string codeScreenText = "enter code";
    [SerializeField] Renderer indicator;
    [SerializeField] Color solvedColor = Color.green;

    ILockableDoor door;
    bool solved;

    void Awake()
    {
        door = doorOverride as ILockableDoor;
        if (door == null) door = GetComponentInParent<ILockableDoor>();
        if (door == null) Debug.LogWarning($"{name}: geen deur gevonden voor deze codelock", this);
    }

    //public override string GetPrompt(GameObject interactor) => codeScreenText;

    // turned off when unlocked or already unlocked
    public override bool CanInteract(GameObject interactor) =>
        !solved && door != null && door.IsLocked;

    public override void Interact(GameObject interactor)
    {
        if (CodeLockUI.Instance) CodeLockUI.Instance.Open(this);
        else Debug.LogError("Geen CodeLockUI in de scene!");
    }

    public override float GetHoldTime() => 0f;

    public bool TrySubmit(string code)
    {
        if (door == null || code != door.RequiredCode) return false;

        solved = true;
        door.Unlock();
        if (indicator) indicator.material.color = solvedColor;
        return true;
    }
}