using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class SettingSelector : MonoBehaviour
{
    public enum Setting { Shadow }

    [SerializeField] Setting setting;
    [Tooltip("Shows the name of the current level.")]
    [SerializeField] TMP_Text valueLabel;
    [Tooltip("Next on the last level goes back to the first one.")]
    [SerializeField] bool wrap;
    [SerializeField] UnityEvent<int> onValueChanged = new();

    public UnityEvent<int> OnValueChanged => onValueChanged;

    int value;

    void OnEnable()
    {
        value = Current();
        ShowValue();
    }

    public void Previous() => Select(value - 1);
    public void Next() => Select(value + 1);

    public void Select(int index)
    {
        int count = Options().Length;
        index = wrap ? (index % count + count) % count : Mathf.Clamp(index, 0, count - 1);
        if (index == value) return;

        value = index;
        ShowValue();
        onValueChanged.Invoke(value);
    }

    void ShowValue()
    {
        if (valueLabel) valueLabel.text = Options()[value];
    }

    string[] Options() => setting switch
    {
        _ => Enum.GetNames(typeof(GameSettings.ShadowQuality)),
    };

    int Current() => setting switch
    {
        _ => (int)GameSettings.Shadow,
    };
}
