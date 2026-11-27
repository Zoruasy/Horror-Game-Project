using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 localDirection;
    private float elapsed;
    private float activeDuration;
    private float activeIntensity;
    private float activePositionAmplitude;
    private float activeRotationAmplitude;
    private float activeFrequency;
    private float activeDirectionInfluence;
    private AnimationCurve activeFadeOut;
    private float noiseSeed;

    private void Awake()
    {
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        if (activeDuration <= 0f)
            return;

        elapsed += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsed / activeDuration);
        float envelope = activeFadeOut.Evaluate(normalizedTime) * activeIntensity;
        float noiseTime = elapsed * activeFrequency;

        Vector3 noise = new Vector3(
            SampleNoise(noiseSeed, noiseTime),
            SampleNoise(noiseSeed + 31.7f, noiseTime),
            SampleNoise(noiseSeed + 63.4f, noiseTime)
        );

        Vector3 directionalKick = -localDirection * activeDirectionInfluence;
        Vector3 positionOffset = (noise + directionalKick) * (activePositionAmplitude * envelope);
        Vector3 rotationOffset = new Vector3(
            noise.y,
            noise.x,
            noise.z + localDirection.x * activeDirectionInfluence
        ) * (activeRotationAmplitude * envelope);

        transform.localPosition = restPosition + positionOffset;
        transform.localRotation = restRotation * Quaternion.Euler(rotationOffset);

        if (elapsed >= activeDuration)
        {
            activeDuration = 0f;
            transform.localPosition = restPosition;
            transform.localRotation = restRotation;
        }
    }

    public void TriggerShake(
        float intensity,
        float duration,
        float positionAmplitude,
        float rotationAmplitude,
        float frequency,
        float directionInfluence,
        AnimationCurve fadeOut,
        Vector3 direction = default)
    {
        activeIntensity = Mathf.Max(0f, intensity);
        activeDuration = Mathf.Max(0.01f, duration);
        activePositionAmplitude = Mathf.Max(0f, positionAmplitude);
        activeRotationAmplitude = Mathf.Max(0f, rotationAmplitude);
        activeFrequency = Mathf.Max(0.01f, frequency);
        activeDirectionInfluence = Mathf.Clamp01(directionInfluence);
        activeFadeOut = fadeOut;
        elapsed = 0f;
        localDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
        noiseSeed = Random.Range(0f, 1000f);
    }

    private static float SampleNoise(float seed, float time)
    {
        return Mathf.PerlinNoise(seed, time) * 2f - 1f;
    }

    private void OnDisable()
    {
        activeDuration = 0f;
        transform.localPosition = restPosition;
        transform.localRotation = restRotation;
    }
}
