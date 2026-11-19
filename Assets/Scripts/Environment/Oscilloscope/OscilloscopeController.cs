using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(LineRenderer))]
public class OscilloscopeController : MonoBehaviour
{
    [Header("Levers")]
    public Lever amplitudeLever;
    public Lever frequencyLever;

    [Header("Screen")]
    public int pointCount = 100;
    [Tooltip("Minimum points per period: higher = smoother curve")]
    public int pointsPerPeriod = 40;
    public float screenWidth = 2.0f;
    public float scrollSpeed = 3.0f;

    [Header("Wave")]
    public float maxAmplitude = 0.5f;
    public float minFrequency = 1.0f;
    public float maxFrequency = 10.0f;

    [Serializable]
    public class Target
    {
        [Tooltip("Own GameObject: Unity allows only one LineRenderer per GameObject")]
        public LineRenderer line;
        [Range(0f, 1f)] public float amplitudeRatio = 0.5f;
        [Range(0f, 1f)] public float frequencyRatio = 0.5f;
        public bool onSolvedStuck;
        public UnityEvent onSolved;
        public UnityEvent onQuit;

        [NonSerialized] public bool solved;
        [NonSerialized] public float matchTimer;
    }

    [Header("Targets (same 0..1 scale as the levers)")]
    public List<Target> targets = new List<Target>();

    [Header("Success")]
    [Tooltip("Max ratio difference allowed per lever")]
    [Range(0f, 0.5f)] public float tolerance = 0.05f;
    [Tooltip("Seconds the waves must stay matched")]
    public float holdDuration = 1f;

    public bool AllSolved => targets.All(t => t.solved);

    private LineRenderer lineRenderer;

    private void OnValidate()
    {
        DrawTargets();
    }

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = pointCount;

        for (int i = 0; i < targets.Count; i++)
            if (targets[i].line) targets[i].line.sortingOrder = i;
        lineRenderer.sortingOrder = targets.Count;
    }

    void Update()
    {
        float amplitudeRatio = amplitudeLever ? amplitudeLever.Ratio : 0f;
        float frequencyRatio = frequencyLever ? frequencyLever.Ratio : 0f;

        DrawWave(amplitudeRatio, frequencyRatio, lineRenderer);
        DrawTargets();

        foreach (Target target in targets)
            CheckSolved(target, amplitudeRatio, frequencyRatio);
    }

    void CheckSolved(Target target, float amplitudeRatio, float frequencyRatio)
    {
        if (target.onSolvedStuck && target.solved) return;

        bool matched = Mathf.Abs(amplitudeRatio - target.amplitudeRatio) <= tolerance
                    && Mathf.Abs(frequencyRatio - target.frequencyRatio) <= tolerance;

        if (target.solved)
        {
            if (!matched)
            {
                target.solved = false;
                target.matchTimer = 0;
                target.onQuit?.Invoke();
            }
            return;
        }

        target.matchTimer = matched ? target.matchTimer + Time.deltaTime : 0f;
        if (target.matchTimer < holdDuration) return;

        Debug.Log("Resolved");
        target.solved = true;
        target.onSolved?.Invoke();
    }

    void DrawTargets()
    {
        foreach (Target target in targets)
            if (target.line) DrawWave(target.amplitudeRatio, target.frequencyRatio, target.line);
    }

    void DrawWave(float amplitudeRatio, float frequencyRatio, LineRenderer line)
    {
        float amplitude = amplitudeRatio * maxAmplitude;
        float frequency = Mathf.Lerp(minFrequency, maxFrequency, frequencyRatio);
        float startX = -screenWidth / 2f;

        int points = Mathf.Max(pointCount, Mathf.CeilToInt(frequency * pointsPerPeriod) + 1);
        if (line.positionCount != points)
            line.positionCount = points;

        for (int i = 0; i < points; i++)
        {
            float t = (float)i / (points - 1);
            float x = startX + t * screenWidth;
            float y = Mathf.Sin(t * frequency * Mathf.PI * 2f + Time.time * scrollSpeed) * amplitude;

            line.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}
