using System.Collections;
using UnityEngine;

public class Door : Interactable
{
    [SerializeField] Transform hinge;
    [SerializeField] float openAngle = 90f;
    [SerializeField] float degreesPerSecond = 180f;

    [Header("Hold")]
    [SerializeField] bool requireHold;
    [SerializeField] float holdTime = 3.0f;

    [Header("Lock")]
    [SerializeField] bool locked;
    [SerializeField] string lockedPrompt = "locked";

    bool isOpen;
    Quaternion closedRotation;
    Quaternion openRotation;
    new Coroutine animation;

    void Awake()
    {
        if (!hinge) hinge = transform;
        closedRotation = hinge.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
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
        animation = StartCoroutine(RotateTo(isOpen ? openRotation : closedRotation));
    }

    public void Unlock() => locked = false;
    public void Lock() => locked = true;

    IEnumerator RotateTo(Quaternion target)
    {
        while (Quaternion.Angle(hinge.localRotation, target) > 0.01f)
        {
            hinge.localRotation = Quaternion.RotateTowards(
                hinge.localRotation, target, degreesPerSecond * Time.deltaTime);
            yield return null;
        }
        hinge.localRotation = target;
        animation = null;
    }

    public override float GetHoldTime()
    {
        return requireHold ? holdTime : 0f;
    }
}
