using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zet dit op je crosshair-Image ("pointer" onder HUD Canvas).
/// Toont een stip, en een hand-icoon zodra de PlayerInteractor iets heeft gevonden
/// waarmee je kunt interacteren (dezelfde momenten waarop de interactie-tekst verschijnt).
/// </summary>
[RequireComponent(typeof(Image))]
public class CrosshairUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerInteractor interactor;
    [SerializeField] Image image;

    [Header("Sprites")]
    [Tooltip("Normale crosshair. Leeg = gewoon een wit vierkantje (zoals het eerst was).")]
    [SerializeField] Sprite defaultSprite;
    [Tooltip("Crosshair als je naar iets interactiefs kijkt.")]
    [SerializeField] Sprite interactSprite;

    [Header("Grootte (pixels)")]
    [SerializeField] Vector2 defaultSize = new Vector2(10f, 10f);
    [SerializeField] Vector2 interactSize = new Vector2(36f, 36f);
    [Tooltip("Hoe snel de hand 'oppopt'. Hoger = sneller.")]
    [SerializeField] float popSpeed = 18f;

    [Header("Kleur")]
    [SerializeField] Color availableColor = Color.white;
    [Tooltip("Als je er wel naar kijkt, maar (nog) niet mee kunt interacteren, bijv. een deur die op slot zit.")]
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

        image.raycastTarget = false;   // de crosshair mag nooit muisklikken opvangen
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
            // klein "pop"-effect van klein naar groot
            float t = 1f - Mathf.Exp(-popSpeed * Time.unscaledDeltaTime);
            rect.sizeDelta = Vector2.Lerp(rect.sizeDelta, interactSize, t);
        }
    }

    void SetHand(bool hand)
    {
        showingHand = hand;
        image.sprite = hand ? interactSprite : defaultSprite;
        image.color = availableColor;
        // terug naar de stip: meteen klein, anders zie je even een grote stip
        rect.sizeDelta = defaultSize;
    }

    // Zelfde regel als PlayerInteractor gebruikt voor de prompt-tekst:
    // alleen als er minstens één zichtbare renderer aan het object zit.
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
