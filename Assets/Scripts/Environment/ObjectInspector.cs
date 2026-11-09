using CharacterController;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ObjectInspector : MonoBehaviour
{
    public static ObjectInspector Instance { get; private set; }

    [Header("References")]
    [Tooltip("Camera the object is brought in front of. Main camera if empty.")]
    [SerializeField] Camera cam;
    [Tooltip("Closes the inspection (usually the Interact action). Right click and Escape always close it too.")]
    [SerializeField] InputActionReference closeAction;

    [Header("UI (optional)")]
    [Tooltip("Shown while inspecting.")]
    [SerializeField] GameObject root;
    [SerializeField] TMP_Text hintLabel;

    [Header("Controls")]
    [Tooltip("Degrees per pixel of mouse movement while left click is held.")]
    [SerializeField] float rotationSpeed = 0.3f;
    [Tooltip("Distance change per scroll step, relative to the framing distance.")]
    [SerializeField] float zoomStep = 0.1f;
    [Tooltip("Min / max distance, relative to the framing distance.")]
    [SerializeField] Vector2 zoomRange = new Vector2(0.5f, 1.5f);

    [Header("Framing")]
    [Tooltip("Part of the screen height the object fills when picked up.")]
    [SerializeField, Range(0.1f, 1f)] float screenFill = 0.6f;
    [Tooltip("How fast the object moves to the camera and back.")]
    [SerializeField] float moveSpeed = 10f;

    [Header("Rendering")]
    [Tooltip("Layer the object is moved to while inspected. An overlay camera draws it on top, so it never clips into walls.")]
    [SerializeField] string inspectLayer = "Inspect";

    [Header("Background")]
    [Tooltip("Blur of the world behind the object. 0 disables it.")]
    [SerializeField, Range(0f, 1.5f)] float blur = 1.5f;
    [Tooltip("Exposure removed from the world behind the object.")]
    [SerializeField, Range(0f, 4f)] float darken = 1.5f;
    [SerializeField, Range(0f, 1f)] float vignette = 0.4f;

    Inspectable target;
    bool inspecting;
    bool returning;
    int openedFrame;

    Transform pivot;
    Transform originalParent;
    Vector3 originalPosition;
    Quaternion originalRotation;

    Vector3 localCenter;
    Quaternion objectRotation;
    Quaternion userRotation;
    float baseDistance;
    float minDistance;
    float zoom;

    Collider[] colliders;
    bool[] colliderStates;
    Rigidbody body;
    bool wasKinematic;
    Transform[] layered;
    int[] originalLayers;

    FirstPersonController controller;
    PlayerInteractor interactor;

    int layer = -1;
    bool layerRestored = false;
    Camera overlay;
    Volume volume;
    UniversalAdditionalCameraData camData;
    bool postProcessForced;

    public bool IsOpen => inspecting && !returning;

    void Awake()
    {
        Instance = this;
        if (root) root.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (volume) Destroy(volume.sharedProfile);
    }

    public void Inspect(Inspectable item, GameObject inspector)
    {
        if (!cam) cam = Camera.main;
        if (IsOpen || !item || !cam) return;
        if (returning) FinishReturn();
        SetupRendering();

        target = item;
        inspecting = true;
        openedFrame = Time.frameCount;

        Transform t = item.transform;
        originalParent = t.parent;
        originalPosition = t.localPosition;
        originalRotation = t.localRotation;

        Bounds bounds = item.GetLocalBounds();
        localCenter = bounds.center;
        zoom = 1f;
        userRotation = Quaternion.identity;

        if (!pivot) pivot = new GameObject("InspectPivot").transform;
        pivot.SetParent(cam.transform, false);
        pivot.SetPositionAndRotation(t.TransformPoint(localCenter), cam.transform.rotation);
        t.SetParent(pivot, true);
        objectRotation = item.FaceCamera ? Quaternion.Euler(item.InspectRotation) : t.localRotation;
        FitDistance(bounds, t.localScale, item.InspectSize);

        colliders = t.GetComponentsInChildren<Collider>();
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }
        body = t.GetComponent<Rigidbody>();
        if (body)
        {
            wasKinematic = body.isKinematic;
            body.isKinematic = true;
        }

        if (overlay)
        {
            layered = t.GetComponentsInChildren<Transform>(true);
            originalLayers = new int[layered.Length];
            for (int i = 0; i < layered.Length; i++)
            {
                originalLayers[i] = layered[i].gameObject.layer;
                layered[i].gameObject.layer = layer;
            }
            overlay.fieldOfView = cam.fieldOfView;
            overlay.nearClipPlane = cam.nearClipPlane;
            overlay.enabled = true;
        }
        if (volume && camData && !camData.renderPostProcessing)
        {
            camData.renderPostProcessing = true;
            postProcessForced = true;
        }

        controller = inspector.GetComponentInParent<FirstPersonController>();
        interactor = inspector.GetComponentInParent<PlayerInteractor>();
        if (controller) controller.enabled = false;
        if (interactor) interactor.enabled = false;

        ShowUI(item);
    }

    public void Close()
    {
        if (!IsOpen) return;

        returning = true;
        if (root) root.SetActive(false);
        if (target) target.transform.SetParent(originalParent, true);

        if (controller) controller.enabled = true;
        if (interactor) interactor.enabled = true;
        controller = null;
        interactor = null;
    }

    void Update()
    {
        float k = 1f - Mathf.Exp(-moveSpeed * Time.deltaTime);
        UpdateBackground(k);

        if (!inspecting) return;

        if (!target)
        {
            if (!returning) Close();
            FinishReturn();
            return;
        }

        if (returning)
        {
            UpdateReturn(k);
            return;
        }

        if (Time.frameCount != openedFrame) HandleInput();
        if (returning) return;

        float distance = Mathf.Max(baseDistance * zoom, minDistance);
        pivot.localPosition = Vector3.Lerp(pivot.localPosition, new Vector3(0f, 0f, distance), k);
        pivot.localRotation = Quaternion.Slerp(pivot.localRotation, userRotation, k);

        Transform t = target.transform;
        t.localRotation = Quaternion.Slerp(t.localRotation, objectRotation, k);
        t.localPosition = -(t.localRotation * Vector3.Scale(t.localScale, localCenter));
    }

    void FitDistance(Bounds bounds, Vector3 scale, float size)
    {
        float radius = Mathf.Max(Vector3.Scale(bounds.extents, scale).magnitude, 0.005f);

        float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float tan = Mathf.Min(tanV, tanV * cam.aspect);
        float angle = Mathf.Atan(tan * screenFill * Mathf.Max(size, 0.01f));

        baseDistance = radius / Mathf.Sin(angle);
        minDistance = cam.nearClipPlane + radius;
    }

    void HandleInput()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        bool close = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                     || (closeAction && closeAction.action.WasPressedThisFrame());
        if (close)
        {
            Close();
            return;
        }

        if (mouse == null) return;

        if (mouse.leftButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue() * rotationSpeed;
            userRotation = Quaternion.AngleAxis(-delta.x, Vector3.up)
                           * Quaternion.AngleAxis(delta.y, Vector3.right)
                           * userRotation;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f) zoom = Mathf.Clamp(zoom - Mathf.Sign(scroll) * zoomStep, zoomRange.x, zoomRange.y);
    }

    void UpdateReturn(float k)
    {
        if (!layerRestored)
        {
            layerRestored = !layerRestored;
            if (layered != null)
                for (int i = 0; i < layered.Length; i++)
                    if (layered[i]) layered[i].gameObject.layer = originalLayers[i];
        }
        Transform t = target.transform;
        t.localPosition = Vector3.Lerp(t.localPosition, originalPosition, k);
        t.localRotation = Quaternion.Slerp(t.localRotation, originalRotation, k);

        if ((t.localPosition - originalPosition).sqrMagnitude < 1e-6f && Quaternion.Angle(t.localRotation, originalRotation) < 0.5f)
            FinishReturn();
    }

    void FinishReturn()
    {
        if (target)
        {
            Transform t = target.transform;
            if (t.parent != originalParent) t.SetParent(originalParent, true);
            t.localPosition = originalPosition;
            t.localRotation = originalRotation;
        }

        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i]) colliders[i].enabled = colliderStates[i];
        if (body) body.isKinematic = wasKinematic;

        if (overlay) overlay.enabled = false;

        target = null;
        body = null;
        colliders = null;
        layered = null;
        inspecting = false;
        returning = false;
        layerRestored = false;
    }

    void UpdateBackground(float k)
    {
        if (!volume) return;

        volume.weight = Mathf.Lerp(volume.weight, IsOpen ? 1f : 0f, k);
        if (IsOpen || volume.weight > 0.01f) return;

        volume.weight = 0f;
        if (postProcessForced && camData) camData.renderPostProcessing = false;
        postProcessForced = false;
    }

    void SetupRendering()
    {
        if (layer >= 0 || camData) return;

        camData = cam.GetUniversalAdditionalCameraData();

        layer = LayerMask.NameToLayer(inspectLayer);
        if (layer < 0) Debug.LogWarning($"[Inspect] layer '{inspectLayer}' missing, inspected objects can clip into walls");
        else if (camData.renderType == CameraRenderType.Base)
        {
            int inspectMask = LayerMask.GetMask(inspectLayer);
            cam.cullingMask &= ~inspectMask;

            overlay = new GameObject("InspectCamera").AddComponent<Camera>();
            overlay.transform.SetParent(cam.transform, false);
            overlay.cullingMask = inspectMask;
            overlay.enabled = false;
            var overlayData = overlay.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;
            overlayData.renderPostProcessing = false;
            camData.cameraStack.Add(overlay);
        }

        if (blur <= 0f && darken <= 0f && vignette <= 0f) return;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        if (blur > 0f)
        {
            var dof = profile.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(0f);
            dof.gaussianEnd.Override(1f);
            dof.gaussianMaxRadius.Override(blur);
            dof.highQualitySampling.Override(true);
        }
        if (darken > 0f) profile.Add<ColorAdjustments>(true).postExposure.Override(-darken);
        if (vignette > 0f) profile.Add<Vignette>(true).intensity.Override(vignette);

        var volumeObject = new GameObject("InspectVolume");
        volumeObject.transform.SetParent(transform, false);
        volumeObject.layer = LayerMask.NameToLayer("Default");

        volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 0f;
        volume.sharedProfile = profile;
    }

    void ShowUI(Inspectable item)
    {
        if (!root) return;

        if (hintLabel)
        {
            string key = closeAction ? closeAction.action.GetBindingDisplayString() : null;
            string close = string.IsNullOrEmpty(key) ? "[Esc]" : $"[{key}] / [Esc]";
            hintLabel.text = $"[Left Click] Rotate   [Scroll] Zoom   {close} Close";
        }
        root.SetActive(true);
    }
}
