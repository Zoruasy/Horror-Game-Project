using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyBindingRow : MonoBehaviour
{
    const string KeyboardGroup = "Keyboard&Mouse";

    public static bool IsRebinding => operation != null;

    public static int LastRebindFrame { get; private set; } = -1;

    static InputActionRebindingExtensions.RebindingOperation operation;
    static KeyBindingRow rebindingRow;

    [SerializeField] InputActionReference action;
    [Tooltip("Part of a composite binding, like up / down / left / right for Move. Empty for a simple action.")]
    [SerializeField] string compositePart;
    [SerializeField] TMP_Text keyLabel;
    [SerializeField] string waitingText = "...";

    void OnEnable()
    {
        GameSettings.BindingsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        GameSettings.BindingsChanged -= Refresh;
        if (rebindingRow == this) operation.Cancel();
    }

    InputAction Action => action ? action.action : null;

    int FindBindingIndex(InputAction inputAction)
    {
        var bindings = inputAction.bindings;
        bool wantsPart = !string.IsNullOrEmpty(compositePart);

        for (int i = 0; i < bindings.Count; i++)
        {
            InputBinding b = bindings[i];
            if (b.isComposite) continue;
            if (wantsPart != b.isPartOfComposite) continue;
            if (wantsPart && !string.Equals(b.name, compositePart, System.StringComparison.OrdinalIgnoreCase)) continue;
            if (b.groups == null || !b.groups.Contains(KeyboardGroup)) continue;
            return i;
        }
        return -1;
    }

    void Refresh()
    {
        if (!keyLabel) return;
        InputAction inputAction = Action;
        int index = inputAction != null ? FindBindingIndex(inputAction) : -1;
        keyLabel.text = index >= 0 ? KeyName(inputAction, index) : "-";
    }

    string KeyName(InputAction inputAction, int index)
    {
        string name = inputAction.GetBindingDisplayString(index);
        if (!string.IsNullOrWhiteSpace(name) && (!keyLabel.font || keyLabel.font.HasCharacters(name)))
            return name;

        return InputControlPath.ToHumanReadableString(inputAction.bindings[index].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    public void StartRebind()
    {
        if (IsRebinding) return;

        InputAction inputAction = Action;
        if (inputAction == null) return;
        int index = FindBindingIndex(inputAction);
        if (index < 0) return;

        bool wasEnabled = inputAction.enabled;
        inputAction.Disable();
        if (keyLabel) keyLabel.text = waitingText;

        rebindingRow = this;
        operation = inputAction.PerformInteractiveRebinding(index)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(_ => Finish(inputAction, wasEnabled, true))
            .OnCancel(_ => Finish(inputAction, wasEnabled, false))
            .Start();
    }

    void Finish(InputAction inputAction, bool wasEnabled, bool changed)
    {
        operation.Dispose();
        operation = null;
        rebindingRow = null;
        LastRebindFrame = Time.frameCount;

        if (wasEnabled) inputAction.Enable();
        if (changed) GameSettings.SaveBindings();
        Refresh();
    }
}
