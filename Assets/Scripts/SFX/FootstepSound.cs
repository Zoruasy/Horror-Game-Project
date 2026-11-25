
using UnityEngine;
using FMODUnity;

public class FootstepsSound : MonoBehaviour
{
    [SerializeField] EventReference footsteps;

    void Start()
    {
        AudioManager.Instance.PlaySound(
            footsteps, transform.position
        );
    }
}
