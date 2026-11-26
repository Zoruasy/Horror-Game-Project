using UnityEngine;

/// <summary>
/// Put this on the player and on every enemy. Leaves a footprint every few meters,
/// alternating left and right. Taking off (jumping / falling off something) and landing
/// always leave two footprints side by side.
/// </summary>
public class SnowFootprints : MonoBehaviour
{
    [Header("Feet (optional)")]
    [Tooltip("Foot bones of your model. Leave empty = footprints next to the center of the character.")]
    public Transform leftFoot;
    public Transform rightFoot;
    [Tooltip("On = walking footprints only via Animation Events (function 'Footstep' with string 'Left' or 'Right'). Jumping/landing always works.")]
    public bool useAnimationEvents = false;

    [Header("Steps")]
    [Tooltip("Distance in meters between two footprints.")]
    public float stepDistance = 0.6f;
    [Tooltip("How far left/right of the center (only used when there are no foot bones).")]
    public float footSpacing = 0.14f;

    [Header("Footprint")]
    public float footprintWidth = 0.13f;
    public float footprintLength = 0.30f;
    [Range(0f, 1f)] public float depth = 1f;

    [Header("Jumping / landing")]
    public bool stampOnJumpAndLand = true;
    [Tooltip("Minimum time in the air before it counts as a jump (so stairs and bumps don't count).")]
    public float minAirTime = 0.12f;
    [Tooltip("Landing footprints are slightly bigger, because you come down with your full weight.")]
    public float landingSizeMultiplier = 1.15f;

    [Header("Ground check")]
    public bool requireGrounded = true;
    [Tooltip("Ground layer only, not the layer of this character.")]
    public LayerMask groundLayers = ~0;
    public float groundCheckDistance = 0.3f;

    Vector3 lastStepPos;
    bool leftNext = true;
    Collider col;
    UnityEngine.CharacterController cc; // fully qualified: the project also has a namespace called "CharacterController"

    // jump state
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
        if (requireGrounded && !IsGrounded()) return; // in the air: no footprint

        StampFoot(leftNext);
        leftNext = !leftNext;
    }

    void HandleJumpAndLand()
    {
        bool grounded = IsGroundedTight();

        if (grounded)
        {
            // Landing: two feet side by side
            if (!wasGrounded && airTime >= minAirTime)
            {
                StampBothFeet(FeetPosition(), FlatForward(), landingSizeMultiplier);
                lastStepPos = transform.position; // don't immediately add another step on top of it
            }
            airTime = 0f;
            takeoffStamped = false;
            lastGroundedFeet = FeetPosition();
            lastGroundedForward = FlatForward();
        }
        else
        {
            airTime += Time.deltaTime;
            // Take-off: once it's clearly a real jump/fall, stamp where you left the ground
            if (!takeoffStamped && airTime >= minAirTime)
            {
                takeoffStamped = true;
                StampBothFeet(lastGroundedFeet, lastGroundedForward, 1f);
            }
        }

        wasGrounded = grounded;
    }

    // Call this from an Animation Event: Footstep("Left") or Footstep("Right")
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

    // Loose check for normal steps
    bool IsGrounded()
    {
        return (cc != null && cc.isGrounded) || RaycastGround(groundCheckDistance);
    }

    // Tight check for jumping/landing (same idea as the FirstPersonController)
    bool IsGroundedTight()
    {
        return (cc != null && cc.isGrounded) || RaycastGround(0.08f);
    }

    bool RaycastGround(float distance)
    {
        // Start at the bottom of the collider (also works when the pivot is in the middle)
        Vector3 origin = FeetPosition();
        origin.y += 0.1f;
        return Physics.Raycast(origin, Vector3.down, 0.1f + distance,
                               groundLayers, QueryTriggerInteraction.Ignore);
    }
}
