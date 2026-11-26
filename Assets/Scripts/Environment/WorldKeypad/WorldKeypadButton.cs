using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldKeypadButton : Interactable
{
    const float PressDuration = 0.045f;
    const float ReleaseDuration = 0.12f;

    [SerializeField] WorldKeypad keypad;
    [SerializeField] string key;

    Vector3 restPosition;
    Vector3 pressOffset;
    float pressStarted;
    float pressAmount;
    float startingAmount;
    bool animating;

    void Awake()
    {
        restPosition = transform.localPosition;

        var label = GetComponentInChildren<TextMesh>();
        var collider = GetComponent<BoxCollider>();
        if (!label || !collider) return;

        // The label faces the player, with its local forward pointing into the key.
        Vector3 inward = transform.InverseTransformVector(label.transform.forward).normalized;
        Vector3 size = collider.size;
        float thickness = Mathf.Abs(inward.x) * size.x +
            Mathf.Abs(inward.y) * size.y + Mathf.Abs(inward.z) * size.z;
        var localOffset = inward * (thickness * 0.35f);
        pressOffset = transform.parent
            ? transform.parent.InverseTransformVector(transform.TransformVector(localOffset))
            : transform.TransformVector(localOffset);
    }

    public override string GetPrompt(GameObject interactor) => "Press " + key;
    public override bool CanInteract(GameObject interactor) => keypad && !keypad.Accepted;

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;

        startingAmount = pressAmount;
        pressStarted = Time.unscaledTime;
        animating = true;
        keypad.Press(key);
    }

    void Update()
    {
        if (!animating) return;

        float elapsed = Time.unscaledTime - pressStarted;
        if (elapsed < PressDuration)
        {
            pressAmount = Mathf.SmoothStep(startingAmount, 1f, elapsed / PressDuration);
        }
        else
        {
            float releaseProgress = (elapsed - PressDuration) / ReleaseDuration;
            pressAmount = Mathf.SmoothStep(1f, 0f, releaseProgress);
            animating = releaseProgress < 1f;
        }

        transform.localPosition = restPosition + pressOffset * pressAmount;
    }

    void OnDisable()
    {
        if (!animating) return;

        animating = false;
        pressAmount = 0f;
        transform.localPosition = restPosition;
    }
}
