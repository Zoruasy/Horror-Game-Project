
using UnityEngine;
using FMODUnity;

public class BreathingSound : MonoBehaviour
{
    [SerializeField] EventReference breathing;

    void Start()
    {
        AudioManager.Instance.PlaySound(
            breathing, transform.position
        );
    }
}
