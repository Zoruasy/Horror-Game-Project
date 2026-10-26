using Unity.VisualScripting;
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

    private float tempsActuel = 0f;
    private bool isHolded = false;
    private bool holdSucces = false;

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
            interactAction.action.started += OnInteract;
            interactAction.action.canceled += ResetTime;
        }
    }

    void OnDisable()
    {
        if (interactAction)
        {
            interactAction.action.started -= OnInteract;
            interactAction.action.canceled += ResetTime;
        }
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

        if (current && current.CanInteract(gameObject) && isHolded && !holdSucces)
        {
            tempsActuel += Time.deltaTime;

            // float progression = tempsActuel / current.GetHoldedTime();

            if (tempsActuel >= current.GetHoldedTime())
            {
                current.Interact(gameObject);
            }
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
        Debug.Log("Interact");
        if (current && current.CanInteract(gameObject))
        {
            isHolded = true;
            tempsActuel = 0f;
            holdSucces = false;
        }
    }

    void ResetTime(InputAction.CallbackContext ctx)
    {
        Debug.Log("Cancel");
        isHolded = false;
        tempsActuel = 0f;
        holdSucces = false;
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
