using UnityEngine;
using UnityEngine.UI;

public class UIGlitchOverlay : MonoBehaviour
{
    [SerializeField] private GlitchEffects glitch;   // sleep de Player hierin
    [SerializeField] private Image overlay;          // GlitchOverlay
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.5f;  // 50% doorzichtig
    [SerializeField] private float flickerSpeed = 12f;

    private float nextFlickerTime;
    private float flicker = 1f;

    private void Update()
    {
        float i = glitch.Intensity;

        if (i <= 0.01f)
        {
            SetAlpha(0f);
            return;
        }

        // Nieuwe flikkerwaarde in blokjes van tijd, zodat het hakkelig oogt
        if (Time.time >= nextFlickerTime)
        {
            flicker = Random.Range(0.2f, 1f);
            nextFlickerTime = Time.time + 1f / Mathf.Lerp(flickerSpeed * 0.5f, flickerSpeed * 2f, i);
        }

        SetAlpha(maxAlpha * i * flicker);
    }

    private void SetAlpha(float a)
    {
        Color c = overlay.color;
        c.a = a;
        overlay.color = c;
    }
}