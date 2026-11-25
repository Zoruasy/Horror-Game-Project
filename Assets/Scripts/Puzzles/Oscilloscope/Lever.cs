using CharacterController;
using UnityEngine;
using UnityEngine.InputSystem;

public class Lever : Interactable
{
    [Header("Rotation")]
    [SerializeField] Vector3 axis = Vector3.right;
    [SerializeField] float angleMin = -45f;
    [SerializeField] float angleMax = 45f;
    [SerializeField] float startAngle = -45f;

    [Header("Mouse")]
    [SerializeField] float sensitivity = 0.3f;
    [SerializeField] bool invert = false;
    [SerializeField] float smoothingSpeed = 15f;

    float targetAngle;
    float currentAngle;
    bool grabbed;
    Quaternion initialRotation;
    InputAction interactAction;
    FirstPersonController player;

    public float Ratio => Mathf.InverseLerp(angleMin, angleMax, currentAngle);

    void Awake()
    {
        initialRotation = transform.localRotation;
        targetAngle = currentAngle = Mathf.Clamp(startAngle, angleMin, angleMax);
        interactAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Interact") : null;
        ApplyRotation();
    }

    void Update()
    {
        if (grabbed)
        {
            bool held = interactAction != null ? interactAction.IsPressed()
                      : Mouse.current != null && Mouse.current.leftButton.isPressed;

            if (!held)
            {
                Release();
            }
            else if (Mouse.current != null)
            {
                float dy = Mouse.current.delta.ReadValue().y;
                if (invert) dy = -dy;
                targetAngle = Mathf.Clamp(targetAngle + dy * sensitivity * GameSettings.MouseSensitivity, angleMin, angleMax);
            }
        }

        currentAngle = Mathf.Lerp(currentAngle, targetAngle, 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime));
        ApplyRotation();
    }

    void ApplyRotation()
    {
        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, axis);
    }

    public override void Interact(GameObject interactor)
    {
        if (grabbed) return;
        grabbed = true;

        player = interactor.GetComponentInParent<FirstPersonController>();
        if (!player) player = interactor.GetComponentInChildren<FirstPersonController>();
        if (player) player.SetControlsLocked(true);
    }

    void Release()
    {
        grabbed = false;
        if (player) player.SetControlsLocked(false);
        player = null;
    }

    void OnDisable()
    {
        if (grabbed) Release();
    }
}
