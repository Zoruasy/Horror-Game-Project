using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightTrigger : MonoBehaviour
{
    [Tooltip("Leave empty = all children lights")]
    [SerializeField] Light[] lights;
    [SerializeField] string playerTag = "Player";

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>();

        SetLights(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) SetLights(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) SetLights(false);
    }

    void SetLights(bool on)
    {
        foreach (Light l in lights) l.enabled = on;
    }
}