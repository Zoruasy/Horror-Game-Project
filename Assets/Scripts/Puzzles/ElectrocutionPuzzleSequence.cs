using UnityEngine;

public class ElectrocutionPuzzleSequence : Interactable
{
    [SerializeField] VialPuzzleManager vialPuzzle;
    [SerializeField] Transform keySection2;

    bool powerOn;
    bool completed;

    public bool PowerOn => powerOn;

    public bool CanElectrocute =>
        powerOn &&
        !completed &&
        vialPuzzle != null &&
        vialPuzzle.IsSolved;

    void Awake()
    {
        if (keySection2 != null)
            keySection2.gameObject.SetActive(false);
    }

    void Start()
    {
        if (vialPuzzle != null)
        {
            vialPuzzle.SetPowerSource(this);
        }

        if (vialPuzzle == null)
            Debug.LogError($"{nameof(ElectrocutionPuzzleSequence)} on {name} has no vial puzzle assigned.", this);

        if (keySection2 == null)
            Debug.LogError($"{nameof(ElectrocutionPuzzleSequence)} on {name} has no Section 2 key assigned.", this);
    }

    public override string GetPrompt(GameObject interactor)
    {
        return powerOn ? "Breaker is on" : "Turn on breaker";
    }

    public override bool CanInteract(GameObject interactor) => !powerOn;

    public override void Interact(GameObject interactor)
    {
        powerOn = true;
        if (vialPuzzle != null)
            vialPuzzle.Recalculate();

        Debug.Log("Breaker switched on. The blood vials puzzle is now powered.", this);
    }

    public void LogBlockedPuzzleAttempt(string puzzleName, Object puzzlePart)
    {
        Debug.LogWarning($"Cannot interact with the {puzzleName} puzzle: the breaker is off.", puzzlePart);
    }

    public void CompleteElectrocution()
    {
        if (!CanElectrocute)
            return;

        completed = true;
        RevealKeySection2();
    }

    void RevealKeySection2()
    {
        if (keySection2 == null)
            return;

        keySection2.gameObject.SetActive(true);
        Debug.Log("Electrocution completed. Key section 2 is now active.", this);
    }
}
