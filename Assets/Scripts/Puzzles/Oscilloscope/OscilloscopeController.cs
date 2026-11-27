using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class OscilloscopeController : MonoBehaviour
{
    [SerializeField] string progressionId = "oscilloscope";

    [Header("Levers")]
    public Lever amplitudeLever;
    public Lever frequencyLever;

    [Header("Screen")]
    [Tooltip("Minimum points per period: higher = smoother curve")]
    public int pointsPerPeriod = 40;
    [Tooltip("Resize this rectangle (Rect tool): waves and text always fill it")]
    public RectTransform screenArea;
    public float scrollSpeed = 3.0f;
    [Tooltip("Text on the screen, replaces the waves while a target is reached")]
    public TMP_Text screenText;

    [Header("Wave")]
    [Tooltip("Fraction of half the screen height")]
    [Range(0f, 1f)] public float maxAmplitude = 1f;
    public float minFrequency = 1.0f;
    public float maxFrequency = 10.0f;

    [Serializable]
    public class Target
    {
        [Tooltip("Own GameObject: Unity allows only one LineRenderer per GameObject")]
        public LineRenderer line;
        [Range(0f, 1f)] public float amplitudeRatio = 0.5f;
        [Range(0f, 1f)] public float frequencyRatio = 0.5f;
        [Tooltip("Shown on the screen instead of the waves while this target is reached")]
        [TextArea] public string message;
        [Tooltip("Button that removes this issue from the screen")]
        public OscilloscopeButton correctButton;

        [NonSerialized] public bool solved;
        [NonSerialized] public float matchTimer;
        [NonSerialized] public bool issueFixed;
    }

    [Header("Targets (same 0..1 scale as the levers)")]
    public List<Target> targets = new List<Target>();

    [Header("Success")]
    [Tooltip("Max ratio difference allowed per lever")]
    [Range(0f, 0.5f)] public float tolerance = 0.05f;
    [Tooltip("Seconds the waves must stay matched")]
    public float holdDuration = 1f;

    [Header("Completion")]
    [Tooltip("Shown on the screen once every issue is fixed")]
    [TextArea] public string completionMessage = "Sending power to central door";

    public bool Completed => targets.Count > 0 && targets.All(t => t.issueFixed);

    Target ActiveIssue => targets.FirstOrDefault(t => t.solved && !t.issueFixed);

    private LineRenderer lineRenderer;
    private Rect lastRect;
    private Matrix4x4 lastMatrix;

    private void OnValidate()
    {
        for (int i = 0; i < targets.Count; i++)
            if (string.IsNullOrWhiteSpace(targets[i].message))
                targets[i].message = $"ISSUE {i + 1}";

        for (int i = 0; i < targets.Count; i++)
        {
            WarnIfUnreachable(i, "amplitude", targets[i].amplitudeRatio, amplitudeLever);
            WarnIfUnreachable(i, "frequency", targets[i].frequencyRatio, frequencyLever);
        }

        DrawTargets();
    }

    void WarnIfUnreachable(int index, string axis, float ratio, Lever lever)
    {
        if (!lever) return;
        float nearest = Mathf.Round(ratio * lever.MaxStep) / lever.MaxStep;
        if (Mathf.Abs(ratio - nearest) <= tolerance) return;
        Debug.LogWarning($"{name}: target {index + 1} {axis} {ratio:0.###} unreachable by {lever.name} " +
                         $"({lever.MaxStep + 1} breakpoints). Nearest: {nearest:0.###}", this);
    }

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        for (int i = 0; i < targets.Count; i++)
            if (targets[i].line) targets[i].line.sortingOrder = i;
        lineRenderer.sortingOrder = targets.Count;
    }

    void Update()
    {
        float amplitudeRatio = amplitudeLever ? amplitudeLever.Ratio : 0f;
        float frequencyRatio = frequencyLever ? frequencyLever.Ratio : 0f;

        // Edit mode: redraw only when screenArea is resized/moved, nothing else runs
        if (!Application.isPlaying)
        {
            if (!screenArea || (screenArea.rect == lastRect && screenArea.localToWorldMatrix == lastMatrix)) return;
            lastRect = screenArea.rect;
            lastMatrix = screenArea.localToWorldMatrix;
            DrawWave(amplitudeRatio, frequencyRatio, lineRenderer);
            DrawTargets();
            return;
        }

        DrawWave(amplitudeRatio, frequencyRatio, lineRenderer);
        DrawTargets();

        foreach (Target target in targets)
            if (!target.issueFixed) CheckSolved(target, amplitudeRatio, frequencyRatio);

        UpdateScreen();
    }

    public void Press(OscilloscopeButton button)
    {
        Target issue = ActiveIssue;
        if (issue == null || issue.correctButton != button) return;

        issue.issueFixed = true;
        if (Completed) PuzzleProgressionManager.MarkCompleted(progressionId);
    }

    void UpdateScreen()
    {
        if (!screenText) return;

        Target issue = ActiveIssue;
        string text = Completed ? completionMessage : issue?.message;
        bool showText = !string.IsNullOrEmpty(text);

        screenText.gameObject.SetActive(showText);
        if (showText)
        {
            screenText.text = text;
            LineRenderer colorSource = Completed ? lineRenderer : issue.line;
            if (colorSource) screenText.color = colorSource.startColor;
        }

        lineRenderer.enabled = !showText;
        foreach (Target target in targets)
            if (target.line) target.line.gameObject.SetActive(!showText && !target.issueFixed);
    }

    void CheckSolved(Target target, float amplitudeRatio, float frequencyRatio)
    {
        bool matched = Mathf.Abs(amplitudeRatio - target.amplitudeRatio) <= tolerance
                    && Mathf.Abs(frequencyRatio - target.frequencyRatio) <= tolerance;
        target.matchTimer = matched ? target.matchTimer + Time.deltaTime : 0f;
        target.solved = target.matchTimer >= holdDuration;
        if (target.solved && target.correctButton == null) {
            target.issueFixed = true;
            PuzzleProgressionManager.MarkCompleted(progressionId);
        }
    }

    void DrawTargets()
    {
        foreach (Target target in targets)
            if (target.line) DrawWave(target.amplitudeRatio, target.frequencyRatio, target.line);
    }

    void DrawWave(float amplitudeRatio, float frequencyRatio, LineRenderer line)
    {
        if (!screenArea || !line.enabled || !line.gameObject.activeInHierarchy) return;

        Rect rect = screenArea.rect;
        float amplitude = amplitudeRatio * maxAmplitude * rect.height / 2f;
        float frequency = Mathf.Lerp(minFrequency, maxFrequency, frequencyRatio);

        int points = Mathf.CeilToInt(frequency * pointsPerPeriod) + 1;
        if (line.positionCount != points)
            line.positionCount = points;

        for (int i = 0; i < points; i++)
        {
            float t = (float)i / (points - 1);
            float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
            float y = rect.center.y + Mathf.Sin((t - 0.5f) * frequency * Mathf.PI * 2f + Time.time * scrollSpeed) * amplitude;

            Vector3 world = screenArea.TransformPoint(new Vector3(x, y, 0f));
            line.SetPosition(i, line.useWorldSpace ? world : line.transform.InverseTransformPoint(world));
        }
    }
}
