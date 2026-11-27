using UnityEngine;

public class EyeLight : MonoBehaviour
{
    [SerializeField] private EyeController eye;
    [SerializeField] private Light eyeLight;

    private void OnEnable()
    {
        if (!eye || !eyeLight)
            return;

        eye.OnEyeToggled += Apply;
        Apply(eye.EyeIn);
    }

    private void OnDisable()
    {
        if (eye)
            eye.OnEyeToggled -= Apply;
    }

    // Light is only visible when the eye is OUT (during F vision)
    private void Apply(bool eyeIsIn)
    {
        if (eyeLight)
            eyeLight.enabled = !eyeIsIn;
    }
}