using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on your crosshair Image ("pointer" under HUD Canvas).
/// Shows a dot, and a hand icon as soon as the PlayerInteractor has found something
/// you can interact with (the same moments the interaction prompt text appears).
/// </summary>
[RequireComponent(typeof(Image))]
public class CrosshairUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerInteractor interactor;
    [SerializeField] Image image;

    [Header("Sprites")]
    [Tooltip("Normal crosshair. Empty = just a white square (like it was originally).")]
    [SerializeField] Sprite defaultSprite;
    [Tooltip("Crosshair when you are looking at something interactable.")]
    [SerializeField] Sprite interactSprite;

    [Header("Size (pixels)")]
    [SerializeField] Vector2 defaultSize = new Vector2(10f, 10f);
    [SerializeField] Vector2 interactSize = new Vector2(36f, 36f);
    [Tooltip("How fast the hand 'pops' in. Higher = faster.")]
    [SerializeField] float popSpeed = 18f;

    [Header("Color")]
    [SerializeField] Color availableColor = Color.white;
    [Tooltip("When you are looking at it but can't interact with it (yet), e.g. a locked door.")]
    [SerializeField] Color unavailableColor = new Color(1f, 1f, 1f, 0.45f);

    RectTransform rect;
    bool showingHand;
    Interactable cachedTarget;
    Renderer[] cachedRenderers;

    void Awake()
    {
        if (!image) image = GetComponent<Image>();
        rect = (RectTransform)transform;
        if (!interactor) interactor = FindFirstObjectByType<PlayerInteractor>();

        image.raycastTarget = false;   // the crosshair must never block mouse clicks
        image.preserveAspect = true;
        SetHand(false);
    }

    void LateUpdate()
    {
        Interactable target = interactor ? interactor.Current : null;
        bool hand = target && IsVisible(target);

        if (hand != showingHand) SetHand(hand);

        if (hand)
        {
            image.color = target.CanInteract(interactor.gameObject) ? availableColor : unavailableColor;
            // small "pop" effect from small to big
            float t = 1f - Mathf.Exp(-popSpeed * Time.unscaledDeltaTime);
            rect.sizeDelta = Vector2.Lerp(rect.sizeDelta, interactSize, t);
        }
    }

    void SetHand(bool hand)
    {
        showingHand = hand;
        image.sprite = hand ? interactSprite : defaultSprite;
        image.color = availableColor;
        // back to the dot: shrink immediately, otherwise you briefly see a big dot
        rect.sizeDelta = defaultSize;
    }

    // Same rule PlayerInteractor uses for the prompt text:
    // only if the object has at least one visible renderer.
    bool IsVisible(Interactable target)
    {
        if (target != cachedTarget)
        {
            cachedTarget = target;
            cachedRenderers = target.GetComponentsInChildren<Renderer>();
        }
        foreach (Renderer r in cachedRenderers)
            if (r && r.enabled) return true;
        return false;
    }
}
