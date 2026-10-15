using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text label;

    [Header("Apparence")]
    [SerializeField] Color availableColor = Color.white;
    [SerializeField] Color unavailableColor = new Color(1f, 1f, 1f, 0.45f);

    string lastPrompt;
    bool lastCanInteract;

    void Awake() => Hide();

    public void Show(string prompt, string key, bool canInteract)
    {
        if (!root.activeSelf) root.SetActive(true);

        if (prompt == lastPrompt && canInteract == lastCanInteract) return;
        lastPrompt = prompt;
        lastCanInteract = canInteract;

        label.text = canInteract && !string.IsNullOrEmpty(key) ? $"[{key}] {prompt}" : prompt;

        label.color = canInteract ? availableColor : unavailableColor;
    }

    public void Hide()
    {
        if (root && root.activeSelf) root.SetActive(false);
        lastPrompt = null;
    }
}
