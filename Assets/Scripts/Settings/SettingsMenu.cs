using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FirstPersonController = CharacterController.FirstPersonController;

public class SettingsMenu : MonoBehaviour
{
    const string PrefabPath = "SettingsMenu";

    public static SettingsMenu Instance { get; private set; }

    public bool IsOpen { get; private set; }

    [Tooltip("Shown while the menu is open (usually the Canvas child).")]
    [SerializeField] GameObject window;

    [Tooltip("Seconds the scroll takes to reach a section.")]
    [SerializeField] float scrollDuration = 0.25f;

    bool otherUiWasOpen;

    float previousTimeScale = 1f;
    CursorLockMode previousLockState;
    bool previousCursorVisible;
    readonly List<Behaviour> disabledWhileOpen = new List<Behaviour>();
    Coroutine scrollRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance) return;

        var prefab = Resources.Load<SettingsMenu>(PrefabPath);
        if (!prefab)
        {
            Debug.LogWarning($"SettingsMenu: no prefab at Resources/{PrefabPath}.");
            return;
        }

        var menu = Instantiate(prefab);
        menu.name = prefab.name;
        DontDestroyOnLoad(menu.gameObject);
    }

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (window) window.SetActive(false);
    }

    void Start()
    {
        GameSettings.ApplyAudio();
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsOpen) Close();
    }

    void Update()
    {
        // Escape is also the key that cancels a key change.
        if (KeyBindingRow.IsRebinding || KeyBindingRow.LastRebindFrame == Time.frameCount) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !otherUiWasOpen)
            Toggle();
    }

    void LateUpdate()
    {
        otherUiWasOpen = (ObjectInspector.Instance && ObjectInspector.Instance.IsOpen)
                         || (CodeLockUI.Instance && CodeLockUI.Instance.IsOpen);
    }


    // ---- Window ----

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (window) window.SetActive(true);
        ScrollToTop();
        EnsureEventSystem();

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        disabledWhileOpen.Clear();
        DisableAll<FirstPersonController>();
        DisableAll<PlayerInteractor>();
        DisableAll<EyeController>();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (window) window.SetActive(false);
        GameSettings.Save();

        Time.timeScale = previousTimeScale;
        AudioListener.pause = false;

        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        foreach (Behaviour b in disabledWhileOpen)
            if (b) b.enabled = true;
        disabledWhileOpen.Clear();
    }

    public void ScrollTo(RectTransform section)
    {
        if (!section) return;
        var scroll = section.GetComponentInParent<ScrollRect>();
        if (!scroll || !scroll.content) return;

        Canvas.ForceUpdateCanvases();
        RectTransform content = scroll.content;
        RectTransform viewport = scroll.viewport ? scroll.viewport : (RectTransform)scroll.transform;

        var corners = new Vector3[4];
        section.GetWorldCorners(corners);
        float sectionTop = content.InverseTransformPoint(corners[1]).y;
        float contentTop = content.rect.yMax;
        float maxScroll = Mathf.Max(0f, content.rect.height - viewport.rect.height);
        float target = Mathf.Clamp(contentTop - sectionTop, 0f, maxScroll);

        scroll.StopMovement();
        if (scrollRoutine != null) StopCoroutine(scrollRoutine);
        scrollRoutine = StartCoroutine(ScrollRoutine(content, target));
    }

    IEnumerator ScrollRoutine(RectTransform content, float target)
    {
        float start = content.anchoredPosition.y;
        for (float t = 0f; t < scrollDuration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / scrollDuration);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Lerp(start, target, k));
            yield return null;
        }
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, target);
        scrollRoutine = null;
    }


    // ---- Settings ----

    public void SetVolume(float value) => GameSettings.SetVolume(value);
    public void SetMusic(float value) => GameSettings.SetMusic(value);
    public void SetSfx(float value) => GameSettings.SetSfx(value);
    public void SetBrightness(float value) => GameSettings.SetBrightness(value);
    public void SetShadow(int level) => GameSettings.SetShadow((GameSettings.ShadowQuality)level);
    public void SetMouseSensitivity(float value) => GameSettings.SetMouseSensitivity(value);
    public void ResetKeys() => GameSettings.ResetBindings();

    public void Exit()
    {
        GameSettings.Save();
        SaveSystem.Save();
        Quit();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    // ---- Helpers ----

    void ScrollToTop()
    {
        if (scrollRoutine != null)
        {
            StopCoroutine(scrollRoutine);
            scrollRoutine = null;
        }

        GameObject root = window ? window : gameObject;
        foreach (ScrollRect scroll in root.GetComponentsInChildren<ScrollRect>(true))
        {
            scroll.StopMovement();
            if (scroll.content)
                scroll.content.anchoredPosition = new Vector2(scroll.content.anchoredPosition.x, 0f);
        }
    }

    void DisableAll<T>() where T : Behaviour
    {
        foreach (T b in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            if (!b.enabled) continue;
            b.enabled = false;
            disabledWhileOpen.Add(b);
        }
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current) return;
        var go = new GameObject("EventSystem", typeof(EventSystem));
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }
}
