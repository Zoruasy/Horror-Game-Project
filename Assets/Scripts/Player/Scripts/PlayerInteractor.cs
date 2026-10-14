using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera cam;
    [SerializeField] InputActionReference interactAction;
    [SerializeField] InteractionPromptUI promptUI;

    [Header("Detection")]
    [SerializeField] float range = 2.5f;
    [SerializeField] float assistRadius = 0.15f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore;

    Interactable current;
    Collider lastCollider;
    Interactable lastColliderInteractable;

    public Interactable Current => current;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void OnEnable()
    {
        if (interactAction)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteract;
        }
    }

    void OnDisable()
    {
        if (interactAction)
            interactAction.action.performed -= OnInteract;
        SetCurrent(null);
    }

    void Update()
    {
        SetCurrent(FindTarget());

        if (current && promptUI)
        {
            string key = interactAction ? interactAction.action.GetBindingDisplayString() : null;
            promptUI.Show(current.GetPrompt(gameObject), key, current.CanInteract(gameObject));
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

        if (current) current.OnLoseFocus();
        current = target;
        if (current) current.OnFocus();

        if (!current && promptUI) promptUI.Hide();
    }

    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (current && current.CanInteract(gameObject))
            current.Interact(gameObject);
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
