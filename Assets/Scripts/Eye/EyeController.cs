using System;
using UnityEngine;

public class EyeController : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.E;
    [SerializeField] private bool eyeIsIn = true;

    public bool EyeIsIn => eyeIsIn;
    public event Action<bool> OnEyeToggled;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetEye(!eyeIsIn);
    }

    public void SetEye(bool value)
    {
        eyeIsIn = value;
        OnEyeToggled?.Invoke(eyeIsIn);
    }
}