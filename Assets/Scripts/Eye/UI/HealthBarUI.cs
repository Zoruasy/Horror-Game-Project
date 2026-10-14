using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private EyeController eye;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider slider;

    private void OnEnable()
    {
        eye.OnEyeToggled += ShowHide;
        health.OnHealthChanged += UpdateBar;

        ShowHide(eye.EyeIsIn);
        UpdateBar(health.Normalized);
    }

    private void OnDisable()
    {
        eye.OnEyeToggled -= ShowHide;
        health.OnHealthChanged -= UpdateBar;
    }

    private void ShowHide(bool eyeIsIn) => panel.SetActive(eyeIsIn);
    private void UpdateBar(float value) => slider.value = value;
}