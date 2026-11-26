using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Connects levers and vials. Adds values of all levers together for the liquid
/// Adds (leverlevel * value). can not exceed max liquid value
/// </summary>
public class VialPuzzleManager : MonoBehaviour
{
    public Vial[] vials;
    public PuzzleLever[] levers;

    public UnityEvent onSolved;
    public UnityEvent onUnsolved;

    public bool IsSolved { get; private set; }

    ElectrocutionPuzzleSequence powerSource;

    void Awake()
    {
        BindLevers();
    }

    public void SetPowerSource(ElectrocutionPuzzleSequence powerSource)
    {
        this.powerSource = powerSource;
        BindLevers();

        Recalculate();
    }

    void BindLevers()
    {
        foreach (var lever in levers)
        {
            if (lever == null) continue;

            lever.SetPowerSource(powerSource);
            lever.SetPuzzleManager(this);
        }
    }

    void OnEnable()
    {
        foreach (var lever in levers)
            if (lever != null) lever.Changed += Recalculate;
    }

    void OnDisable()
    {
        foreach (var lever in levers)
            if (lever != null) lever.Changed -= Recalculate;
    }

    void Start()
    {
        Recalculate();
    }

    public void Recalculate()
    {
        for (int v = 0; v < vials.Length; v++)
        {
            float sum = 0f;
            foreach (var lever in levers)
                if (lever != null) sum += lever.GetContribution(v);

            vials[v].SetLevel(sum);
        }

        bool solved = powerSource != null && powerSource.PowerOn;
        foreach (var vial in vials)
        {
            if (vial == null || !vial.IsCorrect)
            {
                solved = false;
                break;
            }
        }

        Debug.Log($"{GetPuzzleStatus()} | solved={solved}, breakerOn={powerSource != null && powerSource.PowerOn}", this);

        if (solved != IsSolved)
        {
            IsSolved = solved;
            if (solved)
            {
                foreach (var lever in levers)
                    if (lever != null) lever.Lock();

                Debug.Log("Blood vials puzzle solved.", this);
                Debug.Log("All vial levers are now locked.", this);
                onSolved?.Invoke();
            }
            else
            {
                Debug.Log("Blood vials puzzle is no longer solved.", this);
                onUnsolved?.Invoke();
            }
        }
    }

    string GetPuzzleStatus()
    {
        string status = "Vials:";
        for (int i = 0; i < vials.Length; i++)
        {
            Vial vial = vials[i];
            string vialName = vial != null && vial.transform.parent != null
                ? vial.transform.parent.name
                : vial != null ? vial.name : $"vial[{i}]";
            status += vial == null
                ? $" vial[{i}]=MISSING"
                : $" {vialName}={vial.CurrentLevel:0.##}/{vial.targetLevel:0.##}";
        }

        status += " | Levers:";
        for (int i = 0; i < levers.Length; i++)
        {
            PuzzleLever lever = levers[i];
            status += lever == null
                ? $" Button {i + 1}=MISSING"
                : $" {lever.name}={lever.CurrentStep + 1}/{lever.breakpoints} (index {lever.CurrentStep}, adds [{lever.GetContribution(0):0.##}, {lever.GetContribution(1):0.##}, {lever.GetContribution(2):0.##}])";
        }

        return status;
    }
}