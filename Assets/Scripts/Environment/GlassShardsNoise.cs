using UnityEngine;

//put this on the glass shards object, together with a collider that covers the area of the shards.
//when the player steps onto it (walking, sprinting or crouching), every enemy within noiseRange gets
//suspicious and walks to this spot.
//we check the player's feet ourselves instead of using OnTriggerEnter, because the CharacterController
//floats a bit above the ground (skin width) and often never touches a flat trigger on the floor.
[RequireComponent(typeof(Collider))]
public class GlassShardsNoise : MonoBehaviour
{
    [Header("Noise")]
    [Tooltip("Enemies within this many meters hear the glass and get suspicious.")]
    [SerializeField] private float noiseRange = 15f;
    [Tooltip("Seconds before the same shards can make noise again, so walking back and forth doesn't spam the enemy.")]
    [SerializeField] private float cooldown = 1f;

    [Header("Player Check")]
    [Tooltip("How high above the top of the collider the player's feet may be and still count as standing on the shards.")]
    [SerializeField] private float detectionHeight = 0.5f;
    [Tooltip("The player's CharacterController. Auto-found through the 'Player' tag if left empty.")]
    [SerializeField] private UnityEngine.CharacterController player;

    [Header("Sound Effect (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip crunchSound;
    [SerializeField, Range(0f, 1f)] private float crunchVolume = 1f;

    private Collider shardsCollider;
    private bool playerWasOnShards = false;
    private float lastNoiseTime = float.NegativeInfinity;

    private void Reset()
    {
        //make the collider a trigger automatically when the script is added, so the player doesn't bump into it
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        shardsCollider = GetComponent<Collider>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.GetComponentInChildren<UnityEngine.CharacterController>();
        }

        if (player == null)
            Debug.LogWarning("GlassShardsNoise: no player CharacterController found (tag 'Player').", this);
    }

    private void Update()
    {
        if (player == null || shardsCollider == null)
            return;

        bool playerOnShards = isPlayerOnShards();

        //only make noise the moment the player steps on, not every frame while standing on it
        if (playerOnShards && !playerWasOnShards && Time.time - lastNoiseTime >= cooldown)
        {
            lastNoiseTime = Time.time;
            MakeNoise();
        }

        playerWasOnShards = playerOnShards;
    }

    private bool isPlayerOnShards()
    {
        Bounds shards = shardsCollider.bounds;
        Bounds playerBounds = player.bounds;
        Vector3 feet = new Vector3(playerBounds.center.x, playerBounds.min.y, playerBounds.center.z);

        bool insideHorizontally =
            feet.x >= shards.min.x && feet.x <= shards.max.x &&
            feet.z >= shards.min.z && feet.z <= shards.max.z;

        bool atRightHeight =
            feet.y >= shards.min.y - player.skinWidth &&
            feet.y <= shards.max.y + detectionHeight;

        return insideHorizontally && atRightHeight;
    }

    private void MakeNoise()
    {
        if (crunchSound != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(crunchSound, crunchVolume);
            else
                AudioSource.PlayClipAtPoint(crunchSound, transform.position, crunchVolume);
        }

        Vector3 noisePosition = transform.position;
        float rangeSqr = noiseRange * noiseRange;

        foreach (enemyChase enemy in FindObjectsByType<enemyChase>(FindObjectsSortMode.None))
        {
            if ((enemy.transform.position - noisePosition).sqrMagnitude <= rangeSqr)
                enemy.HearNoiseAt(noisePosition);
        }
    }

    private void OnDrawGizmosSelected() //orange = noise range, yellow = area where the player counts as standing on the shards
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, noiseRange);

        Collider col = shardsCollider != null ? shardsCollider : GetComponent<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            Vector3 center = new Vector3(b.center.x, (b.min.y + b.max.y + detectionHeight) / 2f, b.center.z);
            Vector3 size = new Vector3(b.size.x, b.size.y + detectionHeight, b.size.z);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
