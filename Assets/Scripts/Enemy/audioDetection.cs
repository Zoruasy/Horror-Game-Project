using UnityEngine;

public class audioDetection : MonoBehaviour
{
    public enemyChase enemy;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(" IS IN HEARING SPHERE");
        if (other.CompareTag("Player"))
        {
            enemy.isHearingPlayer = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log(" EXITING HEARING SPHERE");
        if (other.CompareTag("Player"))
        {
            enemy.isHearingPlayer = false;
        }
    }
}