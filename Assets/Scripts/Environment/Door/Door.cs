using System.Collections;
using UnityEngine;

public class Door : Interactable, ILockableDoor
{
    [System.Serializable]
    class Leaf
    {
        public Transform hinge;
        public float openAngle = 90f;
        [HideInInspector] public Quaternion closedRotation;
        [HideInInspector] public Quaternion openRotation;
    }

    [SerializeField] Leaf[] leaves;
    [SerializeField] float degreesPerSecond = 180f;

    [Header("Hold")]
    [SerializeField] bool requireHold;
    [SerializeField] float holdTime = 3.0f;

    [Header("Lock")]
    [SerializeField] LockType lockType = LockType.None;
    [SerializeField] string requiredCode = "1234"; //only for codelocks
    [SerializeField] Key requiredKey; //only for item locks
    [SerializeField] string lockedPrompt = "locked";

    bool locked;

    public string RequiredCode => requiredCode;
    public bool IsLocked => locked;

    bool isOpen;
    new Coroutine animation;

    void Awake()
    {
        locked = lockType != LockType.None;

        if (leaves == null || leaves.Length == 0)
            leaves = new[] { new Leaf { hinge = transform } };

        foreach (Leaf leaf in leaves)
        {
            if (!leaf.hinge) leaf.hinge = transform;
            leaf.closedRotation = leaf.hinge.localRotation;
            leaf.openRotation = leaf.closedRotation * Quaternion.Euler(0f, leaf.openAngle, 0f);
        }
    }

    public override string GetPrompt(GameObject interactor)
    {
        if (locked)
        {
            return lockType == LockType.Item && HasKey(interactor) ? "unlock" : lockedPrompt;
        }
        return isOpen ? "close" : "open";
    }

    public override bool CanInteract(GameObject interactor) => !locked || (lockType == LockType.Item && HasKey(interactor));

    public override void Interact(GameObject interactor)
    {
        if (locked)
        {
            // Codelocked doors can only be opened via code lock
            if (lockType != LockType.Item || !HasKey(interactor)) return;
            interactor.GetComponentInParent<Inventory>().Consume(requiredKey);
            locked = false;
        }

        isOpen = !isOpen;
        if (animation != null) StopCoroutine(animation);
        animation = StartCoroutine(RotateAll(isOpen));
    }

    public void Unlock() => locked = false;
    public void Lock() => locked = true;

    public void CloseAndLock()
    {
        if (isOpen)
        {
            isOpen = false;
            if (animation != null) StopCoroutine(animation);
            animation = StartCoroutine(RotateAll(false));   // DoorSlide: MoveAll(false)
        }
        locked = true;
    }

    bool HasKey(GameObject interactor)
    {
        Inventory inventory = interactor.GetComponentInParent<Inventory>();
        return inventory && inventory.Has(requiredKey);
    }

    IEnumerator RotateAll(bool open)
    {
        bool moving = true;
        while (moving)
        {
            moving = false;
            foreach (Leaf leaf in leaves)
            {
                Quaternion target = open ? leaf.openRotation : leaf.closedRotation;
                if (Quaternion.Angle(leaf.hinge.localRotation, target) > 0.01f)
                {
                    leaf.hinge.localRotation = Quaternion.RotateTowards(
                        leaf.hinge.localRotation, target, degreesPerSecond * Time.deltaTime);
                    moving = true;
                }
            }
            yield return null;
        }

        foreach (Leaf leaf in leaves)
            leaf.hinge.localRotation = open ? leaf.openRotation : leaf.closedRotation;

        animation = null;
    }

    public override float GetHoldTime() => requireHold ? holdTime : 0f;
}