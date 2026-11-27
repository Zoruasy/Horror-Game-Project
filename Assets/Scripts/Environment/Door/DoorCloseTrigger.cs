using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorCloseTrigger : MonoBehaviour
{
    [Tooltip("De deur die dicht moet (Door of DoorSlide)")]
    [SerializeField] Interactable door;
    [SerializeField] string playerTag = "Player";
    [SerializeField] bool triggerOnce = true;

    ILockableDoor lockable;
    bool used;

    void Awake()
    {
        lockable = door as ILockableDoor;
        if (lockable == null)
            Debug.LogWarning($"{name}: geen geldige deur toegewezen", this);

        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (lockable == null || (triggerOnce && used)) return;
        if (!other.CompareTag(playerTag)) return;

        used = true;
        lockable.CloseAndLock();
    }
}