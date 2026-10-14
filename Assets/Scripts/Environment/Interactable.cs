using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected string prompt = "Interagir";

    public virtual string GetPrompt(GameObject interactor) => prompt;

    public virtual bool CanInteract(GameObject interactor) => true;

    public abstract void Interact(GameObject interactor);

    public virtual void OnFocus() { }

    public virtual void OnLoseFocus() { }

}
