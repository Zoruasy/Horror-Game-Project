using UnityEngine;

public class BloodSpatterScript : MonoBehaviour
{
    [SerializeField] private GameObject bloodSpatterVFXPrefab;

    public void SpawnBlood(Vector3 position, Vector3 normal)
    {
        Quaternion rot = Quaternion.LookRotation(normal);
        GameObject fx = Instantiate(bloodSpatterVFXPrefab, position, rot);
        Destroy(fx, 3f);
    }
}
