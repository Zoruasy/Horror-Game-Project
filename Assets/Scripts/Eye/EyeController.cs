using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EyeController : MonoBehaviour
{
    [SerializeField] private bool eyeIsIn = true;

    public bool EyeIsIn => eyeIsIn;
    public event Action<bool> OnEyeToggled;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            SetEye(!eyeIsIn);
    }

    public void SetEye(bool value)
    {
        eyeIsIn = value;
        Debug.Log("Eye is in: " + eyeIsIn);
        OnEyeToggled?.Invoke(eyeIsIn);
    }
}