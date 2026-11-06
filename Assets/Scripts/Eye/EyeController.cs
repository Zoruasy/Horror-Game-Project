using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EyeController : MonoBehaviour
{
    [Header("Eye always in")]
    [SerializeField] private bool defaultEyeIn = true;

    [Header("Vision duration")]
    [SerializeField] private float switchDuration = 3f;

    [Header("Vision Cooldown")]
    [SerializeField] private float cooldownMultiplier = 2f; // cooldown = 2 x flash

    private bool visionActive;

    public bool EyeIn => visionActive ? !defaultEyeIn : defaultEyeIn;
    public bool VisionActive => visionActive;
    public bool IsOnCooldown { get; private set; }
    public bool CanActivate => switchRoutine == null && !IsOnCooldown;

    public float SwitchDuration => switchDuration;
    public float CooldownDuration => switchDuration * cooldownMultiplier;
    public float VisionTimeLeft { get; private set; }
    public float CooldownTimeLeft { get; private set; }

    public event Action<bool> OnEyeToggled;

    private Coroutine switchRoutine;

    private void Update()
    {
        // F only works if not in a vision or on cooldown
        if (CanActivate &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            switchRoutine = StartCoroutine(TemporarySwitch());
        }
    }

    // timer for real world vision
    private IEnumerator TemporarySwitch()
    {
        // Vision Duration
        SetVision(true);
        VisionTimeLeft = SwitchDuration;
        while (VisionTimeLeft > 0f)
        {
            VisionTimeLeft -= Time.deltaTime;
            yield return null;
        }
        VisionTimeLeft = 0f;

        // Vision Cooldown
        SetVision(false);
        IsOnCooldown = true;
        switchRoutine = null;

        CooldownTimeLeft = CooldownDuration;
        while (CooldownTimeLeft > 0f)
        {
            CooldownTimeLeft -= Time.deltaTime;
            yield return null;
        }
        CooldownTimeLeft = 0f;
        IsOnCooldown = false;
    }

    private void SetVision(bool active)
    {
        visionActive = active;
        Debug.Log("Eye is in: " + EyeIn);
        OnEyeToggled?.Invoke(EyeIn);
    }
}