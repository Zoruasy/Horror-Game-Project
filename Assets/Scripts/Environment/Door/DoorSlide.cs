using System.Collections;
using UnityEngine;

public class DoorSlide : Interactable
{
    [System.Serializable]
    class Leaf
    {
        public Transform hinge;
        public Vector3 openMove = new(1.2f, 0, 0);
        [HideInInspector] public Vector3 closedPosition;
        [HideInInspector] public Vector3 openPosition;
    }

    [SerializeField] Leaf[] leaves;
    [SerializeField] float metersPerSecond = 10f;

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
            leaf.closedPosition = leaf.hinge.localPosition;
            leaf.openPosition = leaf.closedPosition + leaf.openMove;
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
        animation = StartCoroutine(MoveAll(isOpen));
    }

    public void Unlock() => locked = false;
    public void Lock() => locked = true;

    bool HasKey(GameObject interactor)
    {
        Inventory inventory = interactor.GetComponentInParent<Inventory>();
        return inventory && inventory.Has(requiredKey);
    }

    IEnumerator MoveAll(bool open)
    {
        bool moving = true;
        while (moving)
        {
            moving = false;
            foreach (Leaf leaf in leaves)
            {
                Vector3 target = open ? leaf.openPosition : leaf.closedPosition;
                if (Vector3.Distance(leaf.hinge.localPosition, target) > 0.001f)
                {
                    leaf.hinge.localPosition = Vector3.MoveTowards(
                        leaf.hinge.localPosition, target, metersPerSecond * Time.deltaTime);
                    moving = true;
                }
            }
            yield return null;
        }

        foreach (Leaf leaf in leaves)
            leaf.hinge.localPosition = open ? leaf.openPosition : leaf.closedPosition;

        animation = null;
    }

    public override float GetHoldTime() => requireHold ? holdTime : 0f;
}