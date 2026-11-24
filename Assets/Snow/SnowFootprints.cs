using UnityEngine;

/// <summary>
/// Zet dit op de player en op elke enemy. Laat om de zoveel meter een voetafdruk achter,
/// afwisselend links en rechts. Bij afzetten (springen/eraf vallen) en landen komen er
/// altijd twee afdrukken naast elkaar.
/// </summary>
public class SnowFootprints : MonoBehaviour
{
    [Header("Voeten (optioneel)")]
    [Tooltip("Voet-bones van je model. Leeg laten = afdrukken naast het midden van het character.")]
    public Transform leftFoot;
    public Transform rightFoot;
    [Tooltip("Aan = lopen alleen via Animation Events (functie 'Footstep' met string 'Left' of 'Right'). Springen/landen werkt altijd.")]
    public bool useAnimationEvents = false;

    [Header("Stappen")]
    [Tooltip("Afstand in meters tussen twee afdrukken.")]
    public float stepDistance = 0.6f;
    [Tooltip("Hoe ver links/rechts van het midden (alleen als er geen voet-bones zijn).")]
    public float footSpacing = 0.14f;

    [Header("Afdruk")]
    public float footprintWidth = 0.13f;
    public float footprintLength = 0.30f;
    [Range(0f, 1f)] public float depth = 1f;

    [Header("Springen / landen")]
    public bool stampOnJumpAndLand = true;
    [Tooltip("Zo lang moet je minstens in de lucht zijn voordat het als sprong telt (zodat trapjes en hobbels niet meetellen).")]
    public float minAirTime = 0.12f;
    [Tooltip("Landingsafdrukken zijn iets groter, want je komt met je gewicht neer.")]
    public float landingSizeMultiplier = 1.15f;

    [Header("Grond check")]
    public bool requireGrounded = true;
    [Tooltip("Alleen de grond-layer, niet de layer van dit character.")]
    public LayerMask groundLayers = ~0;
    public float groundCheckDistance = 0.3f;

    Vector3 lastStepPos;
    bool leftNext = true;
    Collider col;
    UnityEngine.CharacterController cc; // volledig uitgeschreven: je project heeft ook een namespace "CharacterController"

    // sprong-status
    bool wasGrounded = true;
    float airTime;
    bool takeoffStamped;
    Vector3 lastGroundedFeet;
    Vector3 lastGroundedForward = Vector3.forward;

    void Start()
    {
        col = GetComponent<Collider>();
        cc = GetComponent<UnityEngine.CharacterController>();
        lastStepPos = transform.position;
        lastGroundedFeet = FeetPosition();
        lastGroundedForward = FlatForward();
    }

    void Update()
    {
        if (SnowSurface.Instance == null) return;

        if (stampOnJumpAndLand) HandleJumpAndLand();

        if (useAnimationEvents) return;

        Vector3 moved = transform.position - lastStepPos;
        moved.y = 0f;
        if (moved.magnitude < stepDistance) return;

        lastStepPos = transform.position;
        if (requireGrounded && !IsGrounded()) return; // in de lucht: geen afdruk

        StampFoot(leftNext);
        leftNext = !leftNext;
    }

    void HandleJumpAndLand()
    {
        bool grounded = IsGroundedTight();

        if (grounded)
        {
            // Landing: twee voeten naast elkaar
            if (!wasGrounded && airTime >= minAirTime)
            {
                StampBothFeet(FeetPosition(), FlatForward(), landingSizeMultiplier);
                lastStepPos = transform.position; // niet meteen nog een stap er bovenop
            }
            airTime = 0f;
            takeoffStamped = false;
            lastGroundedFeet = FeetPosition();
            lastGroundedForward = FlatForward();
        }
        else
        {
            airTime += Time.deltaTime;
            // Afzet: zodra duidelijk is dat het een echte sprong/val is, afdruk op de plek waar je loskwam
            if (!takeoffStamped && airTime >= minAirTime)
            {
                takeoffStamped = true;
                StampBothFeet(lastGroundedFeet, lastGroundedForward, 1f);
            }
        }

        wasGrounded = grounded;
    }

    // Aan te roepen vanuit een Animation Event: Footstep("Left") of Footstep("Right")
    public void Footstep(string foot)
    {
        if (SnowSurface.Instance == null) return;
        if (requireGrounded && !IsGrounded()) return;
        StampFoot(foot.ToLower().StartsWith("l"));
    }

    void StampFoot(bool left)
    {
        Vector3 forward = FlatForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Transform footBone = left ? leftFoot : rightFoot;
        Vector3 pos;
        if (footBone != null) pos = footBone.position;
        else
        {
            pos = FeetPosition() + right * (left ? -footSpacing : footSpacing);
        }

        SnowSurface.Instance.AddFootprint(pos, forward, footprintWidth, footprintLength, depth);
    }

    void StampBothFeet(Vector3 feetCenter, Vector3 forward, float sizeMultiplier)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        float w = footprintWidth * sizeMultiplier;
        float l = footprintLength * sizeMultiplier;
        SnowSurface.Instance.AddFootprint(feetCenter - right * footSpacing, forward, w, l, depth);
        SnowSurface.Instance.AddFootprint(feetCenter + right * footSpacing, forward, w, l, depth);
    }

    Vector3 FlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
    }

    Vector3 FeetPosition()
    {
        Vector3 p = transform.position;
        if (col != null) p.y = col.bounds.min.y;
        return p;
    }

    // Ruime check voor gewone stappen
    bool IsGrounded()
    {
        return (cc != null && cc.isGrounded) || RaycastGround(groundCheckDistance);
    }

    // Strakke check voor springen/landen (zelfde idee als je FirstPersonController)
    bool IsGroundedTight()
    {
        return (cc != null && cc.isGrounded) || RaycastGround(0.08f);
    }

    bool RaycastGround(float distance)
    {
        // Start onderaan de collider (werkt ook als de pivot in het midden zit)
        Vector3 origin = FeetPosition();
        origin.y += 0.1f;
        return Physics.Raycast(origin, Vector3.down, 0.1f + distance,
                               groundLayers, QueryTriggerInteraction.Ignore);
    }
}
