using UnityEngine;

//put this on a trigger box (e.g. in front of a door). when an enemy touches it,
//the enemy stops moving for good and keeps playing the bonk sound on loop.
[RequireComponent(typeof(Collider))]
public class enemyBonkOnDoorTrigger : MonoBehaviour
{
    [Tooltip("Sound file (mp3/wav/...) looped on the enemy for as long as it stands at the door.")]
    [SerializeField] private AudioClip bonkSound;
    [Range(0f, 1f)]
    [SerializeField] private float bonkVolume = 1f;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        //unity only sends trigger events when one of the two objects has a rigidbody.
        //the enemy doesn't have one, so give the trigger a kinematic one (it won't fall or get pushed).
        if (!TryGetComponent(out Rigidbody body))
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        //only the enemy's own body (the capsule collider on the enemy object itself) counts,
        //so child colliders like the vision box don't set it off
        if (other is CapsuleCollider && !other.isTrigger && other.TryGetComponent(out enemyChase enemy))
            enemy.BonkOnDoor(bonkSound, bonkVolume);
    }
}
