using CharacterController;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Generic sliding lever with breakpoints. Hold Interact on it and drag the mouse up/down:
/// the handle jumps one breakpoint per 'pixelsPerStep'. Player controls are locked while held.
/// Puzzles read CurrentStep / Ratio or listen to Changed; puzzle-specific data goes in a subclass.
/// </summary>
public class Lever : Interactable
{
    [Header("Breakpoints")]
    [SerializeField, Min(2)] int breakpoints = 7;
    [Tooltip("Starting breakpoint (0 = bottom)")]
    [SerializeField] int startStep = 0;

    [Header("Handle")]
    [Tooltip("Part that slides. Empty = this object. Its scene position = start step")]
    [SerializeField] Transform handle;
    [Tooltip("Slide direction, in the handle parent's local space")]
    [SerializeField] Vector3 slideDirection = Vector3.up;
    [Tooltip("Units per breakpoint")]
    [SerializeField] float stepDistance = 0.2f;
    [Tooltip("0 = instant jump")]
    [SerializeField] float slideSpeed = 0f;

    [Header("Drag")]
    [Tooltip("Mouse pixels (up/down) per breakpoint")]
    [SerializeField] float pixelsPerStep = 25f;

    [Header("Events")]
    public UnityEvent onGrab;
    public UnityEvent onRelease;
    [Tooltip("Each time the lever reaches a new breakpoint (e.g. click sound)")]
    public UnityEvent onStepChanged;

    public event System.Action Changed;

    public int CurrentStep { get; private set; }
    public int MaxStep => breakpoints - 1;
    public float Ratio => (float)CurrentStep / MaxStep;
    public bool IsDragging { get; private set; }

    Vector3 handleStartPos;
    float dragAccumulator;
    InputAction interactAction;
    FirstPersonController player;

    protected virtual void Awake()
    {
        if (!handle) handle = transform;
        CurrentStep = Mathf.Clamp(startStep, 0, MaxStep);
        handleStartPos = handle.localPosition - slideDirection.normalized * stepDistance * CurrentStep;
        interactAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Interact") : null;
    }

    void Update()
    {
        if (IsDragging) Drag();

        Vector3 target = handleStartPos + slideDirection.normalized * stepDistance * CurrentStep;
        handle.localPosition = slideSpeed <= 0f
            ? target
            : Vector3.Lerp(handle.localPosition, target, 1f - Mathf.Exp(-slideSpeed * Time.deltaTime));
    }

    void Drag()
    {
        bool held = interactAction != null ? interactAction.IsPressed()
                  : Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (!held) { Release(); return; }
        if (Mouse.current == null) return;

        dragAccumulator += Mouse.current.delta.ReadValue().y * GameSettings.MouseSensitivity;

        while (dragAccumulator >= pixelsPerStep)
        {
            dragAccumulator -= pixelsPerStep;
            if (CurrentStep < MaxStep) SetStep(CurrentStep + 1);
            else dragAccumulator = 0f; // top: don't store extra movement
        }
        while (dragAccumulator <= -pixelsPerStep)
        {
            dragAccumulator += pixelsPerStep;
            if (CurrentStep > 0) SetStep(CurrentStep - 1);
            else dragAccumulator = 0f; // bottom: don't store extra movement
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (IsDragging) return;
        IsDragging = true;
        dragAccumulator = 0f;

        player = interactor.GetComponentInParent<FirstPersonController>();
        if (!player) player = interactor.GetComponentInChildren<FirstPersonController>();
        if (player) player.SetControlsLocked(true);
        onGrab?.Invoke();
    }

    void Release()
    {
        IsDragging = false;
        if (player) player.SetControlsLocked(false);
        player = null;
        onRelease?.Invoke();
    }

    public void SetStep(int step)
    {
        step = Mathf.Clamp(step, 0, MaxStep);
        if (step == CurrentStep) return;
        CurrentStep = step;
        onStepChanged?.Invoke();
        Changed?.Invoke();
    }

    void OnDisable()
    {
        if (IsDragging) Release();
    }
}
