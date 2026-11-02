using System.Collections;
using UnityEngine;

public class Door : Interactable
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
    [SerializeField] bool locked;
    [SerializeField] string lockedPrompt = "locked";
    [SerializeField] Key requiredKey;

    bool isOpen;
    new Coroutine animation;

    void Awake()
    {
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
        if (locked) return HasKey(interactor) ? "unlock" : lockedPrompt;
        return isOpen ? "close" : "open";
    }

    public override bool CanInteract(GameObject interactor) => !locked || HasKey(interactor);

    public override void Interact(GameObject interactor)
    {
        if (locked)
        {
            if (!HasKey(interactor)) return;
            interactor.GetComponentInParent<Inventory>().Consume(requiredKey);
            locked = false;
        }

        isOpen = !isOpen;
        if (animation != null) StopCoroutine(animation);
        animation = StartCoroutine(RotateAll(isOpen));
    }

    public void Unlock() => locked = false;
    public void Lock() => locked = true;

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