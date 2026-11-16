
using UnityEngine;

public class NextLevelTrigger : MonoBehaviour
{
    [SerializeField]
    private string nextScene = "DaanParryScene";

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            if (LevelLoading.Instance != null)
            {
                LevelLoading.Instance.LoadLevel(nextScene);
            }
            else
            {
                Debug.LogError("LevelLoading niet gevonden!");
            }
        }
    }
}
