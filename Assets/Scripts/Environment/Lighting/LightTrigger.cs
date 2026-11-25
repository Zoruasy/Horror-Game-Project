using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightTrigger : MonoBehaviour
{
    [Tooltip("Leave empty = all children lights")]
    [SerializeField] Light[] lights;
    [SerializeField] string playerTag = "Player";
    [SerializeField] string enemyTag = "Enemy";

    int occupants;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>();

        SetLights(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsValid(other)) return;
        occupants++;
        SetLights(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsValid(other)) return;
        occupants = Mathf.Max(0, occupants - 1);
        if (occupants == 0) SetLights(false);
    }

    bool IsValid(Collider other)
    {
        return other.CompareTag(playerTag) || other.CompareTag(enemyTag);
    }

    void SetLights(bool on)
    {
        foreach (Light l in lights) l.enabled = on;
    }
}