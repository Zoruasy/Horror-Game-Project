using UnityEngine;

public class OscilloscopeButton : Interactable
{
    [SerializeField] OscilloscopeController controller;

    void Awake()
    {
        if (!controller) controller = GetComponentInParent<OscilloscopeController>();
    }

    public override bool CanInteract(GameObject interactor) => controller;

    public override void Interact(GameObject interactor) => controller.Press(this);
}
