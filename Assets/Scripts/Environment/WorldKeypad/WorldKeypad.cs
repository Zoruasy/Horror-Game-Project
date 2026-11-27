using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WorldKeypad : MonoBehaviour
{
    [SerializeField] CodeLock puzzleLock;
    [SerializeField] Door codeDoor;
    [Tooltip("Progression ID for a standalone keypad. A linked CodeLock reports its own ID.")]
    [SerializeField] string progressionId = "keypad";
    [SerializeField] string code = "1234";
    [SerializeField] Text display;
    [SerializeField] UnityEvent onAccepted = new UnityEvent();

    string entry = "";
    bool rejected;
    public bool Accepted { get; private set; }

    void Awake() => Show("----", Color.white);

    public void Press(string key)
    {
        if (Accepted) return;

        if (key == "C")
        {
            entry = "";
            rejected = false;
            Show("----", Color.white);
            return;
        }

        if (key == "OK")
        {
            Accepted = entry.Length > 0 && TrySubmit();
            rejected = !Accepted;
            entry = "";
            Show(Accepted ? "OPEN" : "ERROR", Accepted ? Color.green : Color.red);
            if (Accepted)
            {
                if (!puzzleLock) PuzzleProgressionManager.MarkCompleted(progressionId);
                onAccepted.Invoke();
            }
            return;
        }

        if (key == null || key.Length != 1 || key[0] < '0' || key[0] > '9') return;
        if (rejected) entry = "";
        rejected = false;
        int maxDigits = puzzleLock ? puzzleLock.RequiredCodeLength
            : codeDoor ? (codeDoor.RequiredCode?.Length ?? 0)
            : code.Length;
        if (entry.Length >= maxDigits) return;
        entry += key;
        Show(entry, Color.white);
    }

    bool TrySubmit()
    {
        if (puzzleLock) return puzzleLock.TrySubmit(entry);
        if (codeDoor) return codeDoor.TryOpenWithCode(entry);
        return entry == code;
    }

    void Show(string value, Color color)
    {
        if (!display) return;
        display.text = value;
        display.color = color;
    }
}


