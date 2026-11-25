using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Een sleepbare hendel met breakpoints (standen).
/// De speler klikt met de linkermuisknop op de hendel, houdt ingedrukt en sleept omhoog/omlaag.
/// De hendel springt per breakpoint 'stepDistance' units (standaard 0.2) omhoog of omlaag.
/// Elke stand telt 'amountPerStep' op bij de waarde van elke vial.
/// Bijv. 7 breakpoints = stand 0 t/m 6 = 6 sprongen omhoog.
///
/// De hendel heeft een Collider nodig om aangeklikt te kunnen worden.
/// </summary>
public class PuzzleLever : MonoBehaviour
{
    /// <summary>True zolang de speler ergens een hendel vasthoudt. Handig om je camera-look uit te zetten.</summary>
    public static bool IsAnyLeverDragged { get; private set; }

    [Header("Breakpoints")]
    [Min(2)] public int breakpoints = 7;
    [Tooltip("Stand waarmee de hendel begint (0 = onderin).")]
    public int startStep = 0;

    [Header("Effect op de vials")]
    [Tooltip("Per vial: hoeveel de waarde verandert per stand (bijv. 1 of 0.5). Zelfde volgorde als de vials in de VialPuzzleManager. Mag negatief.")]
    public float[] amountPerStep = new float[3];

    [Header("Hendel (met Collider!)")]
    [Tooltip("Het deel dat de speler vastpakt en dat omhoog/omlaag schuift. Zijn positie in de scene = startstand.")]
    public Transform handle;
    [Tooltip("Richting van het schuiven, in lokale ruimte van de parent van de hendel.")]
    public Vector3 slideDirection = Vector3.up;
    [Tooltip("Afstand in units per breakpoint.")]
    public float stepDistance = 0.2f;
    [Tooltip("0 = de hendel springt direct. Hoger = korte vloeiende beweging tussen de standen.")]
    public float slideSpeed = 0f;

    [Header("Slepen")]
    [Tooltip("Hoeveel muisbeweging (in pixels, omhoog/omlaag) nodig is voor één breakpoint.")]
    public float pixelsPerStep = 25f;
    [Tooltip("Hoe ver de speler van de hendel mag staan om hem vast te pakken.")]
    public float maxInteractDistance = 3f;
    public LayerMask interactMask = ~0;

    [Header("Events")]
    public UnityEvent onGrab;
    public UnityEvent onRelease;
    [Tooltip("Wordt aangeroepen elke keer dat de hendel naar een nieuwe stand springt (bijv. voor een klik-geluid).")]
    public UnityEvent onStepChanged;

    /// <summary>Wordt afgevuurd als de stand van de hendel verandert. De VialPuzzleManager luistert hiernaar.</summary>
    public event System.Action Changed;

    public int CurrentStep { get; private set; }
    public int MaxStep => breakpoints - 1;
    public bool IsDragging { get; private set; }

    Vector3 handleStartPos; // positie bij stand 0
    float dragAccumulator;

    void Awake()
    {
        CurrentStep = Mathf.Clamp(startStep, 0, MaxStep);
        if (handle != null)
            handleStartPos = handle.localPosition - slideDirection.normalized * stepDistance * CurrentStep;
    }

    void OnDisable()
    {
        if (IsDragging) EndDrag();
    }

    void Update()
    {
        HandleInput();
        UpdateHandlePosition();
    }

    // ---------------- Input ----------------

    void HandleInput()
    {
        if (!IsDragging)
        {
            if (LeftMouseDown() && TryGrab())
                BeginDrag();
            return;
        }

        if (!LeftMouseHeld())
        {
            EndDrag();
            return;
        }

        // Muis omhoog = hendel omhoog.
        dragAccumulator += MouseDeltaY();

        while (dragAccumulator >= pixelsPerStep)
        {
            dragAccumulator -= pixelsPerStep;
            if (CurrentStep < MaxStep) StepUp();
            else dragAccumulator = 0f; // bovenaan: niet "opsparen"
        }
        while (dragAccumulator <= -pixelsPerStep)
        {
            dragAccumulator += pixelsPerStep;
            if (CurrentStep > 0) StepDown();
            else dragAccumulator = 0f; // onderaan: niet "opsparen"
        }
    }

    bool TryGrab()
    {
        if (handle == null) return false;
        Camera cam = Camera.main;
        if (cam == null) return false;

        // Bij een vergrendelde cursor (first person) gebruiken we het midden van het scherm.
        Vector2 screenPos = Cursor.lockState == CursorLockMode.Locked
            ? new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            : MouseScreenPosition();

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance, interactMask, QueryTriggerInteraction.Ignore))
            return hit.collider.transform == handle || hit.collider.transform.IsChildOf(handle);

        return false;
    }

    void BeginDrag()
    {
        IsDragging = true;
        IsAnyLeverDragged = true;
        dragAccumulator = 0f;
        onGrab?.Invoke();
    }

    void EndDrag()
    {
        IsDragging = false;
        IsAnyLeverDragged = false;
        onRelease?.Invoke();
    }

    // ---------------- Standen ----------------

    public void StepUp() => SetStep(CurrentStep + 1);
    public void StepDown() => SetStep(CurrentStep - 1);

    public void SetStep(int step)
    {
        step = Mathf.Clamp(step, 0, MaxStep);
        if (step == CurrentStep) return;
        CurrentStep = step;
        onStepChanged?.Invoke();
        Changed?.Invoke();
    }

    public float GetContribution(int vialIndex)
    {
        if (vialIndex < 0 || vialIndex >= amountPerStep.Length) return 0f;
        return CurrentStep * amountPerStep[vialIndex];
    }

    void UpdateHandlePosition()
    {
        if (handle == null) return;
        Vector3 target = handleStartPos + slideDirection.normalized * stepDistance * CurrentStep;
        handle.localPosition = slideSpeed <= 0f
            ? target
            : Vector3.Lerp(handle.localPosition, target, slideSpeed * Time.deltaTime);
    }

    // ---------------- Input-helpers (werkt met oude én nieuwe Input System) ----------------

    static bool LeftMouseDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    static bool LeftMouseHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    static float MouseDeltaY()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue().y : 0f;
#else
        // "Mouse Y" is standaard 0.1 per pixel, dus x10 geeft ongeveer pixels.
        return Input.GetAxisRaw("Mouse Y") * 10f;
#endif
    }

    static Vector2 MouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
}

/* using System;
using UnityEngine;

/// <summary>
/// A handle with a couple breakpoints
/// Every step up or down changes the value with 'amountPerVial' (cant be negative or 0.).
/// For example. 7 breakpoints = 6 clicks up from lowest point.
///
/// put this script on an object with a collider (for example the handle) if you want to use the mouse.
/// Otherwise use StepUp() / StepDown().
/// </summary>
public class PuzzleLever : MonoBehaviour
{
    [Header("breakpoints")]
    [Min(2)] public int breakpoints = 7;
    [Tooltip("What breakpoint the handle starts at (0 = bottom).")]
    public int startIndex = 0;

    [Header("Effect per breakpoint")]
    [Tooltip("How much every vial changes, in the hierarchy of the vials in the mmanager. for example 1 of 0.5")]
    public float[] amountPerVial = new float[3];

    [Header("visual")]
    [Tooltip("part that moves up and down")]
    public Transform handle;

    [Tooltip("Afstand tussen twee breakpoints in units.")]
    public float stepSize = 0.1f;
    public float slideSpeed = 10f;

    [Header("Bediening")]
    [Tooltip("Linkermuisknop = omhoog, rechtermuisknop = omlaag (alleen met het oude Input Manager).")]
    public bool useMouseInput = true;

    public event Action Changed;
    public int CurrentIndex { get; private set; }

    Vector3 handleStartPos;

    void Awake()
    {
        CurrentIndex = Mathf.Clamp(startIndex, 0, breakpoints - 1);
        if (handle != null)
        {
            handleStartPos = handle.localPosition;
            handle.localPosition = TargetHandlePosition();
        }
    }

    void Update()
    {
        if (handle == null) return;
        float t = 1f - Mathf.Exp(-slideSpeed * Time.deltaTime);
        handle.localPosition = Vector3.Lerp(handle.localPosition, TargetHandlePosition(), t);
    }

    Vector3 TargetHandlePosition()
    {
        return handleStartPos + Vector3.up * (CurrentIndex * stepSize);
    }

    //How much the lever contributes to a vial
    public float GetContribution(int vialIndex)
    {
        if (vialIndex < 0 || vialIndex >= amountPerVial.Length) return 0f;
        return CurrentIndex * amountPerVial[vialIndex];
    }

    public void StepUp() => SetIndex(CurrentIndex + 1);
    public void StepDown() => SetIndex(CurrentIndex - 1);

    void SetIndex(int index)
    {
        index = Mathf.Clamp(index, 0, breakpoints - 1);
        if (index == CurrentIndex) return;
        CurrentIndex = index;
        Changed?.Invoke();
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    void OnMouseOver()
    {
        if (!useMouseInput) return;
        if (Input.GetMouseButtonDown(0)) StepUp();
        else if (Input.GetMouseButtonDown(1)) StepDown();
    }
#endif
}*/