using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SettingSlider : MonoBehaviour
{
    public enum Setting { Volume = 0, Music = 1, Sfx = 2, Brightness = 3, MouseSensitivity = 4 }

    [SerializeField] Setting setting;
    [Tooltip("Shows the value, like 50%. Optional.")]
    [SerializeField] TMP_Text valueLabel;

    Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        if (setting == Setting.MouseSensitivity)
        {
            slider.minValue = GameSettings.MinSensitivity;
            slider.maxValue = GameSettings.MaxSensitivity;
        }
        else
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
        }
    }

    void OnEnable()
    {
        float value = Current();
        slider.SetValueWithoutNotify(value);
        ShowValue(value);
    }

    public void ShowValue(float value)
    {
        if (!valueLabel) return;
        valueLabel.text = setting == Setting.MouseSensitivity
            ? value.ToString("0.0") + "x"
            : Mathf.RoundToInt(value * 100f) + "%";
    }

    float Current() => setting switch
    {
        Setting.Volume => GameSettings.Volume,
        Setting.Music => GameSettings.Music,
        Setting.Sfx => GameSettings.Sfx,
        Setting.Brightness => GameSettings.Brightness,
        _ => GameSettings.MouseSensitivity,
    };
}
