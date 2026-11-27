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
    CharacterController.FirstPersonController controller;

    public Interactable Current => current;

    public float HoldRemaining => isHolding && current ? Mathf.Max(0f, current.GetHoldTime() - holdTimer) : 0f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        controller = GetComponent<CharacterController.FirstPersonController>();
    }

    void OnDisable()
    {
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
            promptUI.Show(current.GetPrompt(gameObject), "[LMB]", current.CanInteract(gameObject));
        }
        else if (promptUI)
        {
            promptUI.Hide();
        }

        UpdateHold();

        if (promptUI) promptUI.SetHoldRemaining(HoldRemaining);

        if (!controller && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryConsumePrimaryClick();
    }

    void UpdateHold()
    {
        if (!isHolding || !current) return;

        if (Mouse.current == null || !Mouse.current.leftButton.isPressed || !current.CanInteract(gameObject))
        {
            CancelHold();
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
        if (!cam) return null;

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
        CancelHold();

        if (current) current.OnLoseFocus();
        current = target;
        if (current) current.OnFocus();

        if (!current && promptUI) promptUI.Hide();
    }

    public bool TryConsumePrimaryClick()
    {
        if (!isActiveAndEnabled) return false;
        if (controlsLocked) return true;

        SetCurrent(FindTarget());
        if (!current) return false;
        if (!current.CanInteract(gameObject)) return true;

        if (current.GetHoldTime() <= 0f)
            current.Interact(gameObject);
        else
        {
            isHolding = true;
            holdTimer = 0f;
        }

        return true;
    }

    void CancelHold()
    {
        isHolding = false;
        holdTimer = 0f;
    }

    public void SetControlsLocked(bool locked)
    {
        controlsLocked = locked;
        if (locked) CancelHold();
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
