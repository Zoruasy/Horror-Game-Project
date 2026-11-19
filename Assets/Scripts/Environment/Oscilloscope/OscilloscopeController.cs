using UnityEngine;

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

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = pointCount;
    }

    void Update()
    {
        float amplitudeRatio = amplitudeLever ? amplitudeLever.Ratio : 0f;
        float frequencyRatio = frequencyLever ? frequencyLever.Ratio : 0f;

        float amplitude = amplitudeRatio * maxAmplitude;
        float frequency = Mathf.Lerp(minFrequency, maxFrequency, frequencyRatio);

        DrawWave(amplitude, frequency);
    }

    void DrawWave(float amplitude, float frequency)
    {
        float startX = -screenWidth / 2f;

        int points = Mathf.Max(pointCount, Mathf.CeilToInt(frequency * pointsPerPeriod) + 1);
        if (lineRenderer.positionCount != points)
            lineRenderer.positionCount = points;

        for (int i = 0; i < points; i++)
        {
            float t = (float)i / (points - 1);
            float x = startX + t * screenWidth;
            float y = Mathf.Sin(t * frequency * Mathf.PI * 2f + Time.time * scrollSpeed) * amplitude;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}
