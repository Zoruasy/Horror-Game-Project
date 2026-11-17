
using UnityEngine;

public class NextLevelTrigger : MonoBehaviour
{
    [SerializeField] private string nextScene = "Scaling";

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger touched by: " + other.name);

        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected!");

            if (LevelLoading.Instance != null)
            {
                triggered = true;

                Debug.Log("Loading scene: " + nextScene);
                LevelLoading.Instance.LoadLevel(nextScene);
            }
            else
            {
                Debug.LogError("LevelLoading not found!");
            }
        }
    }
}
