namespace CharacterController
{
    using UnityEngine;

    public class DetectionSphere : MonoBehaviour
    {
        [Header("References")]
        public FirstPersonController player;
        public Transform detectionSphere;

        [Header("Detection Settings")]
        public float idleRadius = 0f;
        public float walkingRadius = 10f;
        public float crouchingRadius = 0f;
        public float sprintingRadius = 15f;

        [Header("Noise when landing")]
        [SerializeField] private float minLandingVelocity = 1f; // minimum fall speed that makes a landing audible
        [SerializeField] private float landingRadius = 15f;
        [SerializeField] private float landingNoiseDuration = 1f; // how long the landing radius is held before it shrinks

        [Header("Enemy Detection")]
        public LayerMask enemyMask = ~0; //enemy layer

        [Header("Smooth Transition")]
        public float transitionSpeed = 16f;

        [Header("Spawn")]
        [Tooltip("How many seconds after spawning/respawning the player makes no noise at all (radius forced to 0).")]
        [SerializeField] private float spawnSilenceDuration = 2f;

        private float currentRadius;
        private bool wasGrounded = true;
        private float lastAirborneYVelocity;
        private float landingNoiseTimer;
        private float spawnSilenceTimer;

        public float SpawnSilenceDuration => spawnSilenceDuration;

        private void Awake()
        {
            ForceSilence(spawnSilenceDuration);
        }

        private void Start()
        {
            if (player == null)
                player = GetComponent<FirstPersonController>();

            ForceSilence(spawnSilenceDuration);
        }

        //forces the radius to 0 for a while, e.g. after (re)spawning so falling onto the spawn point isn't heard
        public void ForceSilence(float duration)
        {
            spawnSilenceTimer = Mathf.Max(spawnSilenceTimer, duration);
            currentRadius = 0f;
            landingNoiseTimer = 0f;
            wasGrounded = true; //don't count the first landing after spawning as a loud landing
            lastAirborneYVelocity = 0f;

            if (detectionSphere != null)
                detectionSphere.localScale = Vector3.zero;
        }

        private void Update()
        {
            if (player == null || detectionSphere == null)
                return;

            //just spawned: completely silent, skip all noise and enemy checks
            if (spawnSilenceTimer > 0f)
            {
                spawnSilenceTimer -= Time.deltaTime;
                currentRadius = 0f;
                landingNoiseTimer = 0f;
                wasGrounded = player.isGrounded;
                lastAirborneYVelocity = 0f;
                detectionSphere.localScale = Vector3.zero;
                return;
            }

            // Check the player's current state
            bool isCrouching =
                player.CurrentState is PlayerCrouchingState;

            bool isSprinting =
                !isCrouching &&
                player.input.sprint &&
                player.input.moveInput.y > 0f;

            bool isWalking =
                player.CurrentState is PlayerGroundedState &&
                player.input.moveInput.sqrMagnitude > 0.01f;

            // Determine the target detection radius
            float targetRadius = idleRadius;

            

            if (isCrouching)
            {
                targetRadius = crouchingRadius;
                // Debug.Log("Crouching radius: " + targetRadius);
            }
            else if (isSprinting)
            {
                targetRadius = sprintingRadius;
                // Debug.Log("Sprinting radius: " + targetRadius);
            }
            else if (isWalking)
            {
                targetRadius = walkingRadius;
                // Debug.Log("Walking radius: " + targetRadius);
            }
            else
            {
                // Not crouching, sprinting or walking: keeps the idleRadius default from above
                // Debug.Log("Idle radius: " + targetRadius);
            }

            CheckForLanding();

            // Hold the landing radius until the timer runs out, unless the current movement is louder
            if (landingNoiseTimer > 0f)
            {
                landingNoiseTimer -= Time.deltaTime;
                targetRadius = Mathf.Max(targetRadius, landingRadius);
            }

            // Smoothly change the detection radius
            currentRadius = Mathf.MoveTowards(
                currentRadius,
                targetRadius,
                transitionSpeed * Time.deltaTime
            );

            // Unity's default sphere has a diameter of 1
            float diameter = currentRadius * 2f;

            detectionSphere.localScale = new Vector3(
                diameter,
                diameter,
                diameter
            );

           
            CheckForEnemies();


            Debug.Log(targetRadius);//has to be LAST
        }

        private void CheckForLanding()
        {
            if (!player.isGrounded)
            {
                // The grounded state resets moveDirection.y on landing, so remember the speed while still in the air
                lastAirborneYVelocity = player.moveDirection.y;
            }
            else if (!wasGrounded && -lastAirborneYVelocity > minLandingVelocity)
            {
                // Jump straight to the landing radius and keep it there for landingNoiseDuration
                currentRadius = Mathf.Max(currentRadius, landingRadius);
                landingNoiseTimer = landingNoiseDuration;
                Debug.Log("Landing radius: " + currentRadius);
            }

            wasGrounded = player.isGrounded;
        }

        private void CheckForEnemies()
        {
            
            Collider[] hits = Physics.OverlapSphere(
                detectionSphere.position,
                currentRadius,
                enemyMask,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hits)
            {
                
                enemyChase enemy = hit.GetComponentInParent<enemyChase>();

                if (enemy != null)
                {
                    // this executes when the enemy detects the players sound
                    enemy.isHearingPlayer(player.transform);
                }
            }
        }

        public float GetDetectionRadius()
        {
            return currentRadius;
        }
    }
}