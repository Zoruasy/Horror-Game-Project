using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EyeController : MonoBehaviour
{
    [Header("Eye always in")]
    [SerializeField] private bool defaultEyeIn = true;
    [Header("Vision duration")]
    [SerializeField] private float switchDuration = 4f;
    [Header("Vision Cooldown")]
    [SerializeField] private float cooldownDuration = 4f;

    private bool visionActive;
    public bool EyeIn => visionActive ? !defaultEyeIn : defaultEyeIn;
    public bool IsOnCooldown { get; private set; }
    public bool CanActivate => switchRoutine == null && !IsOnCooldown;
    public event Action<bool> OnEyeToggled;

    private Coroutine switchRoutine;

    private void Update()
    {
        // E only works if not in a vision
        if (CanActivate &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            switchRoutine = StartCoroutine(TemporarySwitch());
        }
    }

    //timer for real world vision
    private IEnumerator TemporarySwitch()
    {
        // Vision Duration
        SetVision(true);
        yield return new WaitForSeconds(switchDuration);

        // Vision Cooldown
        SetVision(false);
        IsOnCooldown = true;
        switchRoutine = null;

        yield return new WaitForSeconds(cooldownDuration);
        IsOnCooldown = false;
    }

    private void SetVision(bool active)
    {
        visionActive = active;
        Debug.Log("Eye is in: " + EyeIn);
        OnEyeToggled?.Invoke(EyeIn);
    }
}