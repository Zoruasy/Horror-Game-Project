using UnityEngine;

public class CodeLock : Interactable
{
    [Tooltip("Leave empty = automatically searches door in parents")]
    [SerializeField] Interactable doorOverride;
    [SerializeField] string progressionId = "code-lock";
    [SerializeField] string codeScreenText = "enter code";
    [SerializeField] Renderer indicator;
    [SerializeField] Color solvedColor = Color.green;

    ILockableDoor door;
    bool solved;

    public int RequiredCodeLength => door?.RequiredCode?.Length ?? 0;

    void Awake()
    {
        door = doorOverride as ILockableDoor;
        if (door == null) door = GetComponentInParent<ILockableDoor>();
        if (door == null) Debug.LogWarning($"{name}: No door found for this code lock", this);
    }

    //public override string GetPrompt(GameObject interactor) => codeScreenText;

    // turned off when unlocked or already unlocked
    public override bool CanInteract(GameObject interactor) =>
        !solved && door != null && door.IsLocked;

    public override void Interact(GameObject interactor)
    {
        if (CodeLockUI.Instance) CodeLockUI.Instance.Open(this);
        else Debug.LogError("No CodeLockUI in this scene!");
    }

    public override float GetHoldTime() => 0f;

    public bool TrySubmit(string code)
    {
        if (door == null || code != door.RequiredCode) return false;

        solved = true;
        door.Unlock();
        if (indicator) indicator.material.color = solvedColor;
        PuzzleProgressionManager.MarkCompleted(progressionId);
        return true;
    }
}
