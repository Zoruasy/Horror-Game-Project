using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CodeLockUI : MonoBehaviour
{
    public static CodeLockUI Instance { get; private set; }

    public bool IsOpen => current != null;

    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text display;
    [SerializeField] Button[] digitButtons;      // 10 stuks, index = cijfer
    [SerializeField] Button enterButton;
    [SerializeField] Button clearButton;
    [SerializeField] Button closeButton;
    [SerializeField] int maxDigits = 4;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color wrongColor = Color.red;
    [SerializeField] float wrongFlashTime = 0.5f;

    [Header("Uitzetten zolang de codelock open is")]
    [Tooltip("Player movement, camera look en de interact-script")]
    [SerializeField] Behaviour[] disableWhileOpen;

    CodeLock current;
    string input = "";
    bool busy;
    CursorLockMode previousLockState;
    bool previousCursorVisible;

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < digitButtons.Length; i++)
        {
            int digit = i;
            digitButtons[i].onClick.AddListener(() => AddDigit(digit));
        }
        enterButton.onClick.AddListener(Submit);
        clearButton.onClick.AddListener(Clear);
        closeButton.onClick.AddListener(Close);

        panel.SetActive(false);
    }

    void Update()
    {
        if (current == null) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
#else
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
    }

    public void Open(CodeLock codeLock)
    {
        current = codeLock;
        input = "";
        busy = false;
        Refresh(normalColor);
        panel.SetActive(true);

        foreach (Behaviour b in disableWhileOpen) if (b) b.enabled = false;

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        StopAllCoroutines();
        current = null;
        panel.SetActive(false);

        foreach (Behaviour b in disableWhileOpen) if (b) b.enabled = true;

        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;
    }

    void AddDigit(int digit)
    {
        if (busy || input.Length >= maxDigits) return;
        input += digit;
        Refresh(normalColor);
    }

    void Clear()
    {
        if (busy) return;
        input = "";
        Refresh(normalColor);
    }

    void Submit()
    {
        if (busy || current == null || input.Length == 0) return;

        if (current.TrySubmit(input)) Close();
        else StartCoroutine(WrongRoutine());
    }

    IEnumerator WrongRoutine()
    {
        busy = true;
        Refresh(wrongColor); //flash wrong code
        yield return new WaitForSecondsRealtime(wrongFlashTime);
        input = ""; //reset code field
        Refresh(normalColor);
        busy = false;
    }

    void Refresh(Color color)
    {
        display.color = color;
        display.text = string.Join(" ", input.PadRight(maxDigits, '_').ToCharArray());
    }
}