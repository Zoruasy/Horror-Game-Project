using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected string prompt = "Interact";

    public virtual string GetPrompt(GameObject interactor) => prompt;

    public virtual bool CanInteract(GameObject interactor) => true;

    public abstract void Interact(GameObject interactor);

    public virtual void OnFocus() { }

    public virtual void OnLoseFocus() { }

    public virtual float GetHoldedTime() => 0f;
}
