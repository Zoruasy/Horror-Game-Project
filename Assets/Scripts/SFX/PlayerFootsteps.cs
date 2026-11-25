using UnityEngine;
using FMODUnity;

public class PlayerFootsteps : MonoBehaviour
{
    [SerializeField] UnityEngine.CharacterController controller;
    [SerializeField] EventReference footsteps;
    [SerializeField] float stepInterval = 0.45f;

    float timer;

    void Update()
    {
        if (controller == null || AudioManager.Instance == null)
            return;

        Vector3 velocity = controller.velocity;
        velocity.y = 0;

        bool walking = controller.isGrounded &&
                       velocity.magnitude > 0.1f;

        if (!walking)
        {
            timer = 0;
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            AudioManager.Instance.PlaySound(
                footsteps, transform.position
            );

            timer = stepInterval;
        }
    }
}
