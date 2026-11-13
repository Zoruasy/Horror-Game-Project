using CharacterController;
using UnityEngine;

public class VisionTurningSE : MonoBehaviour
{
    [SerializeField] private FirstPersonController playerController;

    [Header("Trigger")]
    [SerializeField] private Collider turnTrigger;
    [SerializeField] private Transform lookTarget;

    [Header("Turn")]
    [SerializeField] private float turnDuration = 0.5f;
    [SerializeField, Min(0f)] private float returnDelay = 1f;
    [Tooltip("X is normalized time; Y is turn progress from 0 to 1. The same curve is played backward to return.")]
    [SerializeField] private AnimationCurve turnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Zoom")]
    [Tooltip("How many degrees to reduce the camera's field of view while turned toward the target.")]
    [SerializeField, Min(0f)] private float zoomAmount = 15f;
    [SerializeField, Min(0f)] private float zoomDuration = 0.5f;
    [SerializeField, Min(0f)] private float zoomReturnDelay = 1f;
    [Tooltip("X is normalized time; Y is zoom progress from 0 to 1. This curve is played backward to zoom out.")]
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isTurning;
    private bool isReturning;
    private float turnTimer;
    private bool isZooming;
    private bool isZoomReturning;
    private float zoomTimer;
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
        if (playerController == null || isTurning || isZooming)
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
        zoomTimer = 0f;
        isZoomReturning = false;
        isZooming = true;
    }

    private void Update()
    {
        if (isTurning)
            UpdateTurn();

        if (isZooming)
            UpdateZoom();
    }

    private void UpdateTurn()
    {
        turnTimer += Time.deltaTime;

        if (!isReturning)
        {
            float normalizedTime = turnDuration <= 0f ? 1f : Mathf.Clamp01(turnTimer / turnDuration);
            ApplyTurnProgress(Mathf.Clamp01(turnCurve.Evaluate(normalizedTime)));

            if (normalizedTime >= 1f)
            {
                isReturning = true;
                turnTimer = -returnDelay;
            }

            return;
        }

        if (turnTimer < 0f)
        {
            ApplyTurnProgress(1f);
            turnTimer += Time.deltaTime;
            return;
        }

        float returnTime = turnDuration <= 0f ? 1f : Mathf.Clamp01(turnTimer / turnDuration);
        ApplyTurnProgress(Mathf.Clamp01(turnCurve.Evaluate(1f - returnTime)));

        if (returnTime >= 1f)
        {
            ApplyTurnProgress(0f);
            playerController.SetPitchFromExternal(startPitch);
            isReturning = false;
            isTurning = false;
        }
    }

    private void UpdateZoom()
    {
        zoomTimer += Time.deltaTime;

        if (!isZoomReturning)
        {
            float normalizedTime = zoomDuration <= 0f ? 1f : Mathf.Clamp01(zoomTimer / zoomDuration);
            ApplyZoomProgress(Mathf.Clamp01(zoomCurve.Evaluate(normalizedTime)));

            if (normalizedTime >= 1f)
            {
                isZoomReturning = true;
                zoomTimer = -zoomReturnDelay;
            }

            return;
        }

        if (zoomTimer < 0f)
        {
            ApplyZoomProgress(1f);
            zoomTimer += Time.deltaTime;
            return;
        }

        float returnTime = zoomDuration <= 0f ? 1f : Mathf.Clamp01(zoomTimer / zoomDuration);
        ApplyZoomProgress(Mathf.Clamp01(zoomCurve.Evaluate(1f - returnTime)));

        if (returnTime >= 1f)
        {
            ApplyZoomProgress(0f);
            isZoomReturning = false;
            isZooming = false;
        }
    }

    private void ApplyTurnProgress(float progress)
    {
        playerController.transform.rotation =
            Quaternion.Slerp(startBodyRotation, targetBodyRotation, progress);
        playerController.playerCamera.localRotation =
            Quaternion.Slerp(startCameraRotation, targetCameraRotation, progress);
    }

    private void ApplyZoomProgress(float progress)
    {
        playerController.SetVisionZoom(zoomAmount * progress);
    }
}
