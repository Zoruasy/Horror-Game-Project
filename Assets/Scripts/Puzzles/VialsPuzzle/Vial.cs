using UnityEngine;

/// <summary>
/// A vial. The liquid is a cilinder for which the Y-scale gets adjusted by: original Y-scale * (niveau / maxLevel).
/// Put the pivot of the cilinder at the bottom or it will grow wrong.
/// This script can be on the cilinder or on another object with the cilinder in the "liquid" field
/// </summary>
public class Vial : MonoBehaviour
{
    [Header("Puzzle")]
    [Tooltip("Highest value (when full). Standard 10.")]
    public float maxLevel = 10f;
    [Tooltip("The value where the line is (0 t/m maxLevel).")]
    public float targetLevel = 5f;
    [Tooltip("How close the level needs to be")]
    public float tolerance = 0.01f;

    [Header("Visual")]
    [Tooltip("The liquid-cilinder, leave empty if this script is on the cilinder")]
    public Transform liquid;
    [Tooltip("How fast the fluid fills. 0 = direct.")]
    public float fillSpeed = 4f;

    public float CurrentLevel { get; private set; }
    public bool IsCorrect => Mathf.Abs(CurrentLevel - targetLevel) <= tolerance;

    float baseScaleY;
    float visualLevel;

    void Awake()
    {
        if (liquid == null) liquid = transform;
        baseScaleY = liquid.localScale.y; // This is the base scale, should be 1.
        ApplyVisual();
    }

    void Update()
    {
        if (Mathf.Approximately(visualLevel, CurrentLevel)) return;

        visualLevel = fillSpeed <= 0f
            ? CurrentLevel
            : Mathf.MoveTowards(visualLevel, CurrentLevel, fillSpeed * Time.deltaTime);

        ApplyVisual();
    }

    public void SetLevel(float level)
    {
        // Rounding to remove errors
        CurrentLevel = Mathf.Round(Mathf.Clamp(level, 0f, maxLevel) * 1000f) / 1000f;
    }

    void ApplyVisual()
    {
        Vector3 s = liquid.localScale;
        s.y = Mathf.Max(0.0001f, baseScaleY * (visualLevel / maxLevel));
        liquid.localScale = s;
    }
}