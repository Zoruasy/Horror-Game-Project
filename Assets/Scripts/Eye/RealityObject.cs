using System.Collections.Generic;
using UnityEngine;

public enum RevealMode
{
    VisibleNormally, // propaganda, fake walls, hidden passage walls (hidden during flash)
    VisibleOnlyInFlash // enemies, weapons, keycards (shown during flash)
}

public class RealityObject : MonoBehaviour
{
    public static readonly List<RealityObject> All = new();
    public static bool FlashActive;   // set by VisibilityController

    [SerializeField] private RevealMode mode;
    [SerializeField] private Renderer[] renderers; // the real visuals
    [SerializeField] private Collider[] collidersToToggle; // for example hidden passage: walkable during flash

    [Header("Glitch (when NOT in flash)")]
    [SerializeField] private bool triggersGlitch;
    [SerializeField] private float glitchRadius = 6f;
    [SerializeField] private Renderer[] glitchRenderers; // shimmer shown over invisible enemies

    public bool TriggersGlitch => triggersGlitch;
    public float GlitchRadius => glitchRadius;

    private void Reset() => renderers = GetComponentsInChildren<Renderer>();

    private void OnEnable() { All.Add(this); Apply(FlashActive); }
    private void OnDisable() { All.Remove(this); }

    public void Apply(bool flash)
    {
        bool visible = mode == RevealMode.VisibleOnlyInFlash ? flash : !flash;

        foreach (var r in renderers) if (r) r.enabled = visible;
        foreach (var c in collidersToToggle) if (c) c.enabled = mode == RevealMode.VisibleOnlyInFlash ? flash : !flash;
        if (flash) SetGlitch(0f);
    }

    // 0 = no glitch, 1 = strongest
    public void SetGlitch(float intensity)
    {
        foreach (var g in glitchRenderers)
        {
            if (!g) continue;
            g.enabled = intensity > 0.01f;
            g.material.SetFloat("_Intensity", intensity);
        }
    }
}