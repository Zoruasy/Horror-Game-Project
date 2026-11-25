
using UnityEngine;

public class Killbox : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object is the player
        UnityEngine.CharacterController controller =
            other.GetComponentInParent<UnityEngine.CharacterController>();

        if (controller == null || !controller.CompareTag("Player"))
            return;

        if (respawnPoint == null)
        {
            Debug.LogError("Respawn Point is not assigned!");
            return;
        }

        // disable player because of the teleport
        bool wasEnabled = controller.enabled;
        controller.enabled = false;

        // going to spawnpoint
        controller.transform.SetPositionAndRotation(
            respawnPoint.position,
            respawnPoint.rotation
        );

        // enabling the character again
        controller.enabled = wasEnabled;

        Debug.Log("Player killed by hazard goo goo ga!");
    }
}
