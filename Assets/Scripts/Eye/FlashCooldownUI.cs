using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlashCooldownUI : MonoBehaviour
{
    [SerializeField] private EyeController eye;     // Player
    [SerializeField] private Slider slider;         // FlashSlider
    [SerializeField] private Image fillImage;       // Slider -> Fill Area -> Fill
    [SerializeField] private TMP_Text timerText;    // FlashTimerText

    [Header("Kleuren")]
    [SerializeField] private Color flashColor = Color.cyan;
    [SerializeField] private Color cooldownColor = Color.red;
    [SerializeField] private Color readyColor = Color.green;

    private void Update()
    {
        if (eye.VisionActive)
        {
            // empty from left to right: 1 -> 0
            slider.value = eye.VisionTimeLeft / eye.SwitchDuration;
            fillImage.color = flashColor;
            timerText.text = Mathf.CeilToInt(eye.VisionTimeLeft).ToString();
        }
        else if (eye.IsOnCooldown)
        {
            // fill from left to right: 0 -> 1
            slider.value = 1f - eye.CooldownTimeLeft / eye.CooldownDuration;
            fillImage.color = cooldownColor;
            timerText.text = Mathf.CeilToInt(eye.CooldownTimeLeft).ToString();
        }
        else
        {
            slider.value = 1f;
            fillImage.color = readyColor;
            timerText.text = "F";
        }
    }
}