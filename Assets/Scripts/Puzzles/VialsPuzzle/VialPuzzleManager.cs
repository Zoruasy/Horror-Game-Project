using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Connects levers and vials. Adds values of all levers together for the liquid
/// Adds (leverlevel * value). can not exceed max liquid value
/// </summary>
public class VialPuzzleManager : MonoBehaviour
{
    [SerializeField] string progressionId = "vials";

    public Vial[] vials;
    public PuzzleLever[] levers;

    public UnityEvent onSolved;
    public UnityEvent onUnsolved;

    public bool IsSolved { get; private set; }

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

        bool solved = true;
        foreach (var vial in vials)
            if (!vial.IsCorrect) { solved = false; break; }

        if (solved != IsSolved)
        {
            IsSolved = solved;
            if (solved)
            {
                if (vials.Length > 0) PuzzleProgressionManager.MarkCompleted(progressionId);
                onSolved?.Invoke();
            }
            else onUnsolved?.Invoke();
        }
    }
}