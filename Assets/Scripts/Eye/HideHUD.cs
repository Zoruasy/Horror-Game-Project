using UnityEngine;

public class HideHUD : MonoBehaviour
{
    [SerializeField] private EyeController eye;
    [SerializeField] private GameObject hudCanvas;

    private void OnEnable()
    {
        eye.OnEyeToggled += Apply;
        Apply(eye.EyeIn);
    }

    private void OnDisable() => eye.OnEyeToggled -= Apply;

    // HUD is hidden when the eye is OUT
    private void Apply(bool eyeIsIn) => hudCanvas.SetActive(eyeIsIn);
}