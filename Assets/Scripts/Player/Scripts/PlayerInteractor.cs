using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera cam;
    [SerializeField] InteractionPromptUI promptUI;

    [Header("Detection")]
    [SerializeField] float range = 2.5f;
    [SerializeField] float assistRadius = 0.15f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore;

    Interactable current;
    Collider lastCollider;
    Interactable lastColliderInteractable;

    float holdTimer;
    bool isHolding;
    bool controlsLocked;

    InputAction interactAction;

    public Interactable Current => current;

    public float HoldRemaining => isHolding && current ? Mathf.Max(0f, current.GetHoldTime() - holdTimer) : 0f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        interactAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Interact") : null;
    }

    void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.Enable();
            interactAction.started += OnInteract;
            interactAction.canceled += CancelInteract;
        }
    }

    void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.started -= OnInteract;
            interactAction.canceled -= CancelInteract;
        }
        SetCurrent(null);
    }

    void Update()
    {
        if (controlsLocked)
        {
            SetCurrent(null);
            if (promptUI) promptUI.Hide();
            return;
        }

        SetCurrent(FindTarget());

        if (promptUI && current &&
            current.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled))
        {
            string key = interactAction?.GetBindingDisplayString();
            promptUI.Show(current.GetPrompt(gameObject), key, current.CanInteract(gameObject));
        }
        else if (promptUI)
        {
            promptUI.Hide();
        }

        UpdateHold();

        if (promptUI) promptUI.SetHoldRemaining(HoldRemaining);
    }

    void UpdateHold()
    {
        if (!isHolding || !current) return;

        if (!current.CanInteract(gameObject))
        {
            holdTimer = 0f;
            return;
        }

        holdTimer += Time.deltaTime;

        if (holdTimer >= current.GetHoldTime())
        {
            isHolding = false;
            current.Interact(gameObject);
        }
    }

    Interactable FindTarget()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        float maxDistance = range;

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, triggers))
        {
            Interactable target = GetInteractable(hit.collider);
            if (target) return target;

            maxDistance = hit.distance;
        }

        if (assistRadius > 0f &&
            Physics.SphereCast(ray, assistRadius, out RaycastHit sphereHit, maxDistance, hitMask, triggers))
        {
            return GetInteractable(sphereHit.collider);
        }
        return null;
    }

    Interactable GetInteractable(Collider col)
    {
        if (col != lastCollider)
        {
            lastCollider = col;
            lastColliderInteractable = col.GetComponentInParent<Interactable>();
        }
        return lastColliderInteractable;
    }

    void SetCurrent(Interactable target)
    {
        if (target == current) return;

        holdTimer = 0f;

        if (current) current.OnLoseFocus();
        current = target;
        if (current) current.OnFocus();

        if (!current && promptUI) promptUI.Hide();
    }

    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (controlsLocked) return;

        if (current && current.CanInteract(gameObject))
        {
            isHolding = true;
            holdTimer = 0f;
        }
        Debug.Log("INTERACTINTERACTINTERACTINTERACTINTERACTINTERACTINTERACTINTERACTINTERACT");
    }

    void CancelInteract(InputAction.CallbackContext ctx)
    {
        isHolding = false;
        holdTimer = 0f;
    }

    public void SetControlsLocked(bool locked)
    {
        controlsLocked = locked;
        if (locked)
        {
            isHolding = false;
            holdTimer = 0f;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!cam) return;
        Gizmos.color = current ? Color.green : Color.yellow;
        Vector3 start = cam.transform.position;
        Vector3 end = start + cam.transform.forward * range;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, assistRadius);
    }
#endif
}
