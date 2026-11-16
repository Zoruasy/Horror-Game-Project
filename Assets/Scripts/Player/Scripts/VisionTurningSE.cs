using CharacterController;
using UnityEngine;

public class VisionTurningSE : MonoBehaviour
{
    [SerializeField] private FirstPersonController playerController;
    private PlayerInteractor playerInteractor;

    [Header("Trigger")]
    [SerializeField] private Collider turnTrigger;
    [SerializeField] private Transform lookTarget;

    [Header("Turn")]
    [SerializeField] private float turnDuration = 2f;
    [SerializeField, Min(0f)] private float returnDelay = 2.5f;
    [Tooltip("X is normalized time; Y is turn progress from 0 to 1. The same curve is played backward to return.")]
    [SerializeField] private AnimationCurve turnCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.75f, 1f, 0f, 0f));

    [Header("Zoom")]
    [Tooltip("How many degrees to reduce the camera's field of view while turned toward the target.")]
    [SerializeField, Min(0f)] private float zoomAmount = 25f;
    [Tooltip("X is normalized time; Y is zoom progress from 0 to 1. This curve is played backward with the turn.")]
    [SerializeField] private AnimationCurve zoomCurve = new AnimationCurve(
        new Keyframe(0.41f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f));

    private bool isTurning;
    private bool isReturning;
    private float turnTimer;
    private float startPitch;
    private Quaternion startBodyRotation;
    private Quaternion targetBodyRotation;
    private Quaternion startCameraRotation;
    private Quaternion targetCameraRotation;

    public bool IsTurning => isTurning;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<FirstPersonController>();

        if (playerController == null)
            Debug.LogError("VisionTurningSE requires a FirstPersonController on the same GameObject.", this);

        playerInteractor = GetComponent<PlayerInteractor>();

        if (turnTrigger != null && !turnTrigger.isTrigger)
            Debug.LogError("The selected turn trigger must have Is Trigger enabled.", turnTrigger);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != turnTrigger)
            return;

        if (lookTarget == null)
        {
            Debug.LogError("Assign a look target to VisionTurningSE.", this);
            return;
        }

        TurnTo(lookTarget.position);
    }

    public void TurnTo(Vector3 worldPoint)
    {
        if (playerController == null || isTurning)
            return;

        Transform playerCamera = playerController.playerCamera;
        if (playerCamera == null)
        {
            Debug.LogError("Assign the player camera on the FirstPersonController.", this);
            return;
        }

        Vector3 direction = worldPoint - playerCamera.position;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Vector3 localDirection = playerController.transform.InverseTransformDirection(direction.normalized);
        float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        float horizontalDistance = Mathf.Sqrt(
            localDirection.x * localDirection.x +
            localDirection.z * localDirection.z);

        float targetPitch = Mathf.Clamp(
            Mathf.Atan2(-localDirection.y, horizontalDistance) * Mathf.Rad2Deg,
            -90f,
            90f);

        startBodyRotation = playerController.transform.rotation;
        targetBodyRotation = startBodyRotation * Quaternion.Euler(0f, yaw, 0f);
        startCameraRotation = playerCamera.localRotation;
        targetCameraRotation = Quaternion.Euler(targetPitch, 0f, 0f);
        startPitch = Mathf.DeltaAngle(0f, startCameraRotation.eulerAngles.x);
        turnTimer = 0f;
        isReturning = false;
        isTurning = true;
        playerController.SetControlsLocked(true);
        if (playerInteractor != null)
            playerInteractor.SetControlsLocked(true);
    }

    private void Update()
    {
        if (!isTurning)
            return;

        turnTimer += Time.deltaTime;

        if (!isReturning)
        {
            float normalizedTime = turnDuration <= 0f ? 1f : Mathf.Clamp01(turnTimer / turnDuration);
            ApplyProgress(
                Mathf.Clamp01(turnCurve.Evaluate(normalizedTime)),
                Mathf.Clamp01(zoomCurve.Evaluate(normalizedTime)));

            if (normalizedTime >= 1f)
            {
                isReturning = true;
                turnTimer = -returnDelay;
            }

            return;
        }

        if (turnTimer < 0f)
        {
            ApplyProgress(1f, 1f);
            turnTimer += Time.deltaTime;
            return;
        }

        float returnTime = turnDuration <= 0f ? 1f : Mathf.Clamp01(turnTimer / turnDuration);
        ApplyProgress(
            Mathf.Clamp01(turnCurve.Evaluate(1f - returnTime)),
            Mathf.Clamp01(zoomCurve.Evaluate(1f - returnTime)));

        if (returnTime >= 1f)
        {
            ApplyProgress(0f, 0f);
            playerController.SetPitchFromExternal(startPitch);
            playerController.SetControlsLocked(false);
            if (playerInteractor != null)
                playerInteractor.SetControlsLocked(false);
            isReturning = false;
            isTurning = false;

            if (turnTrigger != null)
            turnTrigger.enabled = false;
        }
    }

    private void ApplyProgress(float turnProgress, float zoomProgress)
    {
        playerController.transform.rotation =
            Quaternion.Slerp(startBodyRotation, targetBodyRotation, turnProgress);
        playerController.playerCamera.localRotation =
            Quaternion.Slerp(startCameraRotation, targetCameraRotation, turnProgress);
        playerController.SetVisionZoom(zoomAmount * zoomProgress);
    }
}
