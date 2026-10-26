using UnityEngine;

public class GlitchEffects : MonoBehaviour
{
    [SerializeField] private EyeController eye;               // sleep de Player hierin
    [SerializeField] private Material screenGlitchMaterial;   // mag leeg blijven
    [SerializeField] private float smoothing = 6f;

    public float Intensity { get; private set; }

    private void Update()
    {
        float screen = 0f;

        foreach (var obj in RealityObject.All)
        {
            if (!obj.TriggersGlitch) continue;

            if (RealityObject.FlashActive)
            {
                obj.SetGlitch(0f);
                continue;
            }

            float d = Vector3.Distance(transform.position, obj.transform.position);
            float t = Mathf.Clamp01(1f - d / obj.GlitchRadius);

            obj.SetGlitch(t);
            screen = Mathf.Max(screen, t);
        }

        Intensity = Mathf.MoveTowards(Intensity, screen, smoothing * Time.deltaTime);

        if (screenGlitchMaterial)
            screenGlitchMaterial.SetFloat("_Intensity", Intensity);
    }

    private void OnDisable()
    {
        if (screenGlitchMaterial)
            screenGlitchMaterial.SetFloat("_Intensity", 0f);
    }
}