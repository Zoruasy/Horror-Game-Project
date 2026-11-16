using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using PostVolume = UnityEngine.Rendering.Volume;

public static class GameSettings
{
    public enum ShadowQuality { Off, Low, Medium, High }
    public enum AudioChannel { Sfx, Music }

    struct ShadowPreset
    {
        public float distance;
        public int resolution;
        public int cascades;
    }

    static readonly ShadowPreset[] shadowPresets =
    {
        new() { distance = 0f,  resolution = 256,  cascades = 1 },
        new() { distance = 20f, resolution = 1024, cascades = 1 },
        new() { distance = 40f, resolution = 2048, cascades = 2 },
        new() { distance = 80f, resolution = 4096, cascades = 4 },
    };

    const string File = SaveSystem.SettingsFile;

    // Audio
    const string VolumeKey = "volume";
    const string MusicKey = "music";
    const string SfxKey = "sfx";

    // Graphic
    const string BrightnessKey = "brightness";
    const string ShadowKey = "shadow";

    // Control
    const string SensitivityKey = "sensitivity";
    const string BindingsKey = "bindings";

    const string MixerPath = "MainAudioMixer";
    const string MusicGroupName = "Music";
    const string SfxGroupName = "SFX";

    public const string MusicParameter = "MusicVolume";
    public const string SfxParameter = "SFXVolume";

    public const float MinSensitivity = 0.1f;
    public const float MaxSensitivity = 2f;

    // Audio
    public static float Volume { get; private set; } = 1f;
    public static float Music { get; private set; } = 1f;
    public static float Sfx { get; private set; } = 1f;

    // Graphic
    public static float Brightness { get; private set; } = 0.5f;
    public static ShadowQuality Shadow { get; private set; } = ShadowQuality.High;

    // Control
    public static float MouseSensitivity { get; private set; } = 1f;

    public static event Action BindingsChanged;

    static AudioMixer mixer;
    public static AudioMixerGroup MusicGroup { get; private set; }
    public static AudioMixerGroup SfxGroup { get; private set; }
    static PostVolume brightnessVolume;
    static LiftGammaGain brightnessGamma;
    static readonly Dictionary<UniversalRenderPipelineAsset, ShadowPreset> shadowDefaults = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        LoadMixer();
        SetVolume(SaveSystem.Get(File, VolumeKey, 1f));
        SetMusic(SaveSystem.Get(File, MusicKey, 1f));
        SetSfx(SaveSystem.Get(File, SfxKey, 1f));
        Brightness = Mathf.Clamp01(SaveSystem.Get(File, BrightnessKey, 0.5f));
        SetShadow(SaveSystem.Get(File, ShadowKey, ShadowQuality.High));
        SetMouseSensitivity(SaveSystem.Get(File, SensitivityKey, 1f));

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void LoadBindings()
    {
        string json = SaveSystem.Get(File, BindingsKey, "");
        if (!string.IsNullOrEmpty(json) && InputSystem.actions != null)
            InputSystem.actions.LoadBindingOverridesFromJson(json);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyBrightness();
        RouteUnassignedSources();
        ApplyAudio();
    }

    public static void Save() => SaveSystem.Flush(File);


    // ---- Audio ----

    public static void SetVolume(float value)
    {
        Volume = Mathf.Clamp01(value);
        AudioListener.volume = Volume;
        SaveSystem.Set(File, VolumeKey, Volume);
    }

    public static void SetMusic(float value)
    {
        Music = Mathf.Clamp01(value);
        if (mixer) mixer.SetFloat(MusicParameter, ToDecibels(Music));
        SaveSystem.Set(File, MusicKey, Music);
    }

    public static void SetSfx(float value)
    {
        Sfx = Mathf.Clamp01(value);
        if (mixer) mixer.SetFloat(SfxParameter, ToDecibels(Sfx));
        SaveSystem.Set(File, SfxKey, Sfx);
    }

    public static void ApplyAudio()
    {
        if (!mixer) return;
        mixer.SetFloat(MusicParameter, ToDecibels(Music));
        mixer.SetFloat(SfxParameter, ToDecibels(Sfx));
    }

    static void LoadMixer()
    {
        mixer = Resources.Load<AudioMixer>(MixerPath);
        if (!mixer)
        {
            Debug.LogWarning($"GameSettings: no AudioMixer at Resources/{MixerPath}.");
            return;
        }
        MusicGroup = FindGroup(MusicGroupName);
        SfxGroup = FindGroup(SfxGroupName);
    }

    static AudioMixerGroup FindGroup(string groupName)
    {
        foreach (AudioMixerGroup group in mixer.FindMatchingGroups(groupName))
        {
            if (group.name == groupName) return group;
        }
        Debug.LogWarning($"GameSettings: no mixer group '{groupName}' in {mixer.name}.");
        return null;
    }

    public static AudioMixerGroup GetGroup(AudioChannel channel) =>
        channel == AudioChannel.Music ? MusicGroup : SfxGroup;

    public static void Route(AudioSource source, AudioChannel channel = AudioChannel.Sfx)
    {
        if (source) source.outputAudioMixerGroup = GetGroup(channel);
    }

    static void RouteUnassignedSources()
    {
        if (!SfxGroup) return;
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!source.outputAudioMixerGroup) source.outputAudioMixerGroup = SfxGroup;
        }
    }

    // Same as AudioSource.PlayClipAtPoint, but routed through the mixer.
    public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f,
        AudioChannel channel = AudioChannel.Sfx)
    {
        if (!clip) return;

        var go = new GameObject("One shot audio");
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 1f;
        source.volume = volume;
        Route(source, channel);
        source.Play();
        Object.Destroy(go, clip.length * (Time.timeScale < 0.01f ? 0.01f : Time.timeScale));
    }

    static float ToDecibels(float value) => Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;


    // ---- Graphic ----

    public static void SetBrightness(float value)
    {
        Brightness = Mathf.Clamp01(value);
        SaveSystem.Set(File, BrightnessKey, Brightness);
        ApplyBrightness();
    }

    static void ApplyBrightness()
    {
        if (!brightnessVolume)
        {
            var go = new GameObject("[Brightness]");
            Object.DontDestroyOnLoad(go);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            brightnessGamma = profile.Add<LiftGammaGain>();
            brightnessVolume = go.AddComponent<PostVolume>();
            brightnessVolume.isGlobal = true;
            brightnessVolume.priority = 50f;
            brightnessVolume.sharedProfile = profile;
        }

        brightnessGamma.gamma.Override(new Vector4(1f, 1f, 1f, Brightness - 0.5f));

        Camera cam = Camera.main;
        if (!Mathf.Approximately(Brightness, 0.5f) && cam
            && cam.TryGetComponent(out UniversalAdditionalCameraData data))
            data.renderPostProcessing = true;
    }

    public static void SetShadow(ShadowQuality value)
    {
        if (value < ShadowQuality.Off || value > ShadowQuality.High) value = ShadowQuality.High;
        Shadow = value;
        SaveSystem.Set(File, ShadowKey, Shadow);
        ApplyShadow();
    }

    static void ApplyShadow()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp) return;

        if (!shadowDefaults.ContainsKey(urp))
        {
            shadowDefaults[urp] = new ShadowPreset
            {
                distance = urp.shadowDistance,
                resolution = urp.mainLightShadowmapResolution,
                cascades = urp.shadowCascadeCount,
            };
        }

        ApplyPreset(urp, shadowPresets[(int)Shadow]);
    }

    static void ApplyPreset(UniversalRenderPipelineAsset urp, ShadowPreset preset)
    {
        urp.shadowDistance = preset.distance;
        urp.mainLightShadowmapResolution = preset.resolution;
        urp.shadowCascadeCount = preset.cascades;
    }

#if UNITY_EDITOR
    static void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
    {
        if (change != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
        foreach (var pair in shadowDefaults)
        {
            if (pair.Key) ApplyPreset(pair.Key, pair.Value);
        }
        shadowDefaults.Clear();
    }
#endif


    // ---- Control ----

    public static void SetMouseSensitivity(float value)
    {
        MouseSensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
        SaveSystem.Set(File, SensitivityKey, MouseSensitivity);
    }

    public static void SaveBindings()
    {
        if (InputSystem.actions == null) return;
        SaveSystem.Set(File, BindingsKey, InputSystem.actions.SaveBindingOverridesAsJson());
        Save();
    }

    public static void ResetBindings()
    {
        if (InputSystem.actions != null) InputSystem.actions.RemoveAllBindingOverrides();
        SaveSystem.DeleteKey(File, BindingsKey);
        Save();
        BindingsChanged?.Invoke();
    }
}
