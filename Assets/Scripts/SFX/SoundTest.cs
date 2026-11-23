
using UnityEngine;
using FMODUnity;

public class SoundTest : MonoBehaviour
{
    void Start()
    {
        RuntimeManager.PlayOneShot(
            "event:/vfx/char/breathing"
        );
    }
}
