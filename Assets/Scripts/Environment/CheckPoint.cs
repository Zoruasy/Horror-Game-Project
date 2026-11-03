using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckPoint : Interactable
{
    [Tooltip("Where the player respawns. Uses this object's transform if empty.")]
    [SerializeField] Transform spawnPoint;
    [Tooltip("Write the save to disk when activated.")]
    [SerializeField] bool saveOnActivate = true;

    Transform SpawnPoint => spawnPoint ? spawnPoint : transform;

    void OnDrawGizmos()
    {
        Transform point = SpawnPoint;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(point.position + Vector3.up * 0.9f, 0.4f);
        Gizmos.DrawRay(point.position + Vector3.up * 0.9f, point.forward);
    }

    public override void Interact(GameObject interactor)
    {
        PlayerRespawn respawn = interactor.GetComponentInParent<PlayerRespawn>();
        if (!respawn || !respawn.SetSpawn(SpawnPoint)) return;
        if (saveOnActivate) SaveSystem.Save();
    }
}
