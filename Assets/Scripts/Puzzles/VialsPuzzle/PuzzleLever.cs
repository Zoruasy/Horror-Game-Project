using UnityEngine;

/// <summary>
/// Sliding lever for the vial puzzle: each step adds 'amountPerStep' to each vial.
/// E.g. 7 breakpoints = steps 0 to 6.
/// </summary>
public class PuzzleLever : Lever
{
    [Header("Vials")]
    [Tooltip("Per vial: value change per step (e.g. 1 or 0.5). Same order as the vials in VialPuzzleManager. Can be negative.")]
    public float[] amountPerStep = new float[3];

    public float GetContribution(int vialIndex)
    {
        if (vialIndex < 0 || vialIndex >= amountPerStep.Length) return 0f;
        return CurrentStep * amountPerStep[vialIndex];
    }
}
