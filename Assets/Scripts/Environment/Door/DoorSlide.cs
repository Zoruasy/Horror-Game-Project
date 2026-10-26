using System.Collections;
using UnityEngine;

public class DoorSlide : Interactable
{
    [SerializeField] Transform hinge;
    [SerializeField] Vector3 openMove = new(1.2f, 0, 0);
    [SerializeField] float metersPerSecond = 10f;

    [Header("Hold")]
    [SerializeField] bool requireHold;
    [SerializeField] float holdTime = 3.0f;

    [Header("Lock")]
    [SerializeField] bool locked;
    [SerializeField] string lockedPrompt = "locked";

    bool isOpen;
    Vector3 closedPosition;
    Vector3 openPosition;
    new Coroutine animation;

    void Awake()
    {
        if (!hinge) hinge = transform;
        closedPosition = hinge.localPosition;
        openPosition = closedPosition + openMove;
    }

    public override string GetPrompt(GameObject interactor)
    {
        if (locked) return lockedPrompt;
        return isOpen ? "close" : "open";
    }

    public override bool CanInteract(GameObject interactor) => !locked;

    public override void Interact(GameObject interactor)
    {
        isOpen = !isOpen;
        if (animation != null) StopCoroutine(animation);
        animation = StartCoroutine(MoveTo(isOpen ? openPosition : closedPosition));
    }

    public void Unlock() => locked = false;
    public void Lock() => locked = true;

    IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(hinge.localPosition, target) > 0.001f)
        {
            hinge.localPosition = Vector3.MoveTowards(hinge.localPosition, target, metersPerSecond * Time.deltaTime);
            yield return null;
        }
        hinge.localPosition = target;
        animation = null;
    }

    public override float GetHoldTime()
    {
        return requireHold ? holdTime : 0f;
    }
}
