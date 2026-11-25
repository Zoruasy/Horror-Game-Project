using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundCue", menuName = "Audio/Sound Cue")]
public class SoundCue : ScriptableObject
{
    [Tooltip("One of these is picked at random each time the cue plays.")]
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Random pitch between min and max. Keep both at 1 for no variation.")]
    [Range(0.1f, 3f)] public float minPitch = 1f;
    [Range(0.1f, 3f)] public float maxPitch = 1f;

    [Tooltip("Mixer group the cue plays through (Music / SFX volume sliders in Settings).")]
    public GameSettings.AudioChannel output = GameSettings.AudioChannel.Sfx;

    public AudioClip PickClip() =>
        clips == null || clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];

    public void Play(AudioSource source)
    {
        AudioClip clip = PickClip();
        if (!clip || !source) return;

        Setup(source);
        source.clip = clip;
        source.volume = volume;
        source.Play();
    }

    public void PlayOneShot(AudioSource source)
    {
        AudioClip clip = PickClip();
        if (!clip || !source) return;

        Setup(source);
        source.PlayOneShot(clip, volume);
    }

    public void PlayAtPoint(Vector3 position)
    {
        AudioClip clip = PickClip();
        if (!clip) return;

        var go = new GameObject($"One shot audio ({name})");
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        Setup(source);
        source.clip = clip;
        source.volume = volume;
        source.Play();
        Destroy(go, clip.length / Mathf.Max(source.pitch, 0.01f) + 0.1f);
    }

    void Setup(AudioSource source)
    {
        source.outputAudioMixerGroup = GameSettings.GetGroup(output);
        source.pitch = Random.Range(minPitch, maxPitch);
    }

    void OnValidate()
    {
        if (maxPitch < minPitch) maxPitch = minPitch;
    }
}
