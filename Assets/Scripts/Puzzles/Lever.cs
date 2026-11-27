using CharacterController;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Generic sliding lever with breakpoints. Hold the interact button on it and look up/down:
/// the handle follows the player's gaze, snapping to the nearest breakpoint.
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
    [SerializeField] bool isGazeLock = false;
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
    Camera cam;
    FirstPersonController player;

    protected virtual void Awake()
    {
        if (!handle) handle = transform;
        CurrentStep = Mathf.Clamp(startStep, 0, MaxStep);
        handleStartPos = handle.localPosition - slideDirection.normalized * stepDistance * CurrentStep;
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
        if (isGazeLock) DragWithoutGaze();
        else DragWithGaze();
    }

    void DragWithGaze()
    {
        if (!cam) return;

        Transform parent = handle.parent;
        Vector3 origin = parent ? parent.TransformPoint(handleStartPos) : handleStartPos;
        Vector3 stepVec = slideDirection.normalized * stepDistance;
        if (parent) stepVec = parent.TransformVector(stepVec);

        Vector3 dir = cam.transform.forward;
        Vector3 w = origin - cam.transform.position;
        float a = Vector3.Dot(stepVec, stepVec);
        float b = Vector3.Dot(stepVec, dir);
        float denom = a - b * b;
        if (denom < 1e-6f * a) return;

        float steps = (b * Vector3.Dot(dir, w) - Vector3.Dot(stepVec, w)) / denom;
        SetStep(Mathf.RoundToInt(steps));
    }

    void DragWithoutGaze()
    {
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
        if (isGazeLock) dragAccumulator = 0f;
        else cam = Camera.main;

        player = interactor.GetComponentInParent<FirstPersonController>();
        if (!player) player = interactor.GetComponentInChildren<FirstPersonController>();
        LockPlayer(true);
        onGrab?.Invoke();
    }

    public override void StopInteract(GameObject interactor) => Release();

    void Release()
    {
        if (!IsDragging) return;
        IsDragging = false;
        LockPlayer(false);
        player = null;
        onRelease?.Invoke();
    }

    void LockPlayer(bool locked)
    {
        if (!player) return;
        if (isGazeLock) player.SetControlsLocked(locked);
        else player.SetMovementLocked(locked);
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
        Release();
    }
}
