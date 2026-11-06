using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;


public class enemyChase : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public Transform eyePoint;
    [SerializeField] private Transform respawnPoint;

    [Header("Vision Light")]
    [SerializeField] private Light visionLight;
    [SerializeField] private Color normalVisionColor = Color.white;
    [SerializeField] private Color spottedVisionColor = Color.yellow;
    [SerializeField] private Color aggroVisionColor = Color.red;

    [Header("Sound Effects")]
    [Tooltip("Plays the alert sounds. Auto-found (or added) on this enemy if left empty.")]
    [SerializeField] private AudioSource alertAudioSource;
    [Tooltip("Played once when the enemy starts getting suspicious.")]
    [SerializeField] private AudioClip suspiciousSound;
    [Tooltip("Played once when the enemy becomes aggro.")]
    [SerializeField] private AudioClip aggroSound;
    [SerializeField, Range(0f, 1f)] private float suspiciousVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float aggroVolume = 1f;
    private bool wasAggro = false;
    private bool wasSuspicious = false;

    [Header("Vision Settings")]
    public float viewDistance = 10f;
    [Range(0f, 180f)]
    public float viewAngle = 90f;

    [Header("Detection")]
    public LayerMask playerDetectMask;
    [SerializeField] private float keepSuspicionTimerInit = 2f;
    [SerializeField] private float timeBeforeAggroInit = 2f;
    [SerializeField] private float aggroTimerInit = 5f;
    [SerializeField] private float passiveMoveSpeed = 1f;
    [SerializeField] private float alertedMoveSpeed = 3f;

    [Header("Detection Points")]
    [Tooltip("Parent object on the player whose children are the body points. Auto-found as a child of the player named 'DetectionPoints' if left empty.")]
    [SerializeField] private Transform detectionPoints;
    [Tooltip("When this many detection points (or more) are visible, the enemy instantly becomes aggro.")]
    [SerializeField] private int pointsNeededForInstantAggro = 3;
    private Transform detectionPointsOwner; //the player the detection points were looked up for


    [Header("Navigation")]
    private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform patrolPoints;
    [SerializeField] private BoxCollider visionBox;

    private Transform[] patrolPointsArray; //will be filled in start
    private Transform cachedPlayer;
    private Collider[] playerColliders;
    private bool keepsSuspiciousForABit = false;
    float keepSuspicionTimer = 2f;
    float timeBeforeAggro = 2f;
    float aggroTimer = 5f;

    bool isAggro = false;
    bool isSuspicious = false;

    private LevelManager levelManager;
    private bool heardPlayer = false;
    private Vector3 lastHeardPlayerPosition;





    //when detected form the sphere, this function is called by the sphere
    public void isHearingPlayer(Transform playerTransform)
    {
        Debug.Log("HEARING HEARING HEARING");
        player = playerTransform;

        heardPlayer = true;
        lastHeardPlayerPosition = playerTransform.position;



    }



    void Update()
    {
        TryKillPlayerAtCloseRange();
        PlayAlertSounds();

        if (visionLight == null)
            return;

        if (isAggro)
            visionLight.color = aggroVisionColor;
        else if (keepsSuspiciousForABit)
            visionLight.color = spottedVisionColor;
        else
            visionLight.color = normalVisionColor;
    }



    //plays a sound once when the enemy enters the suspicious or aggro state
    private void PlayAlertSounds()
    {
        bool suspiciousNow = keepsSuspiciousForABit && !isAggro;

        if (isAggro && !wasAggro)
        {
            PlayAlertClip(aggroSound, aggroVolume);
        }
        else if (suspiciousNow && !wasSuspicious && !wasAggro) //not when calming down from aggro to suspicious
        {
            PlayAlertClip(suspiciousSound, suspiciousVolume);
        }

        wasAggro = isAggro;
        wasSuspicious = suspiciousNow;
    }

    private void PlayAlertClip(AudioClip clip, float volume)
    {
        if (clip == null || alertAudioSource == null)
            return;

        alertAudioSource.Stop(); //cut off the suspicious sound if it goes straight to aggro
        alertAudioSource.clip = clip;
        alertAudioSource.volume = volume;
        alertAudioSource.Play();
    }

    private void TryKillPlayerAtCloseRange()
    {
        if (player == null || eyePoint == null)
            return;

        int sightMask = Physics.DefaultRaycastLayers & ~(1 << gameObject.layer);
        if (Physics.Raycast(
                eyePoint.position,
                eyePoint.forward,
                out RaycastHit hit,
                0.5f,
                sightMask,
                QueryTriggerInteraction.Ignore) &&
            (hit.transform == player || hit.transform.IsChildOf(player)) &&
            (playerDetectMask.value & (1 << hit.transform.gameObject.layer)) != 0)
        {
            if (levelManager == null)
            {
                Debug.LogError("No LevelManager in scene, can't reset level.", this);
                return;
            }
    SaveSystem.ResetLevel();
SaveSystem.ResetLevel();
            UnityEngine.CharacterController characterController = player.GetComponent<UnityEngine.CharacterController>();
            if (characterController != null)
                characterController.enabled = false;

            player.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);

            if (characterController != null)
                characterController.enabled = true;

            isAggro = false;
            heardPlayer = false;
            keepsSuspiciousForABit = false;
            keepSuspicionTimer = keepSuspicionTimerInit;

            timeBeforeAggro = timeBeforeAggroInit;
            aggroTimer = aggroTimerInit;
            navMeshAgent.speed = passiveMoveSpeed;
            navMeshAgent.ResetPath();
        }
    }


    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (alertAudioSource == null)
            alertAudioSource = GetComponent<AudioSource>();
        if (alertAudioSource == null)
        {
            alertAudioSource = gameObject.AddComponent<AudioSource>();
            alertAudioSource.playOnAwake = false;
            alertAudioSource.spatialBlend = 1f; //3D sound, so the player can hear where the enemy is
            alertAudioSource.rolloffMode = AudioRolloffMode.Linear;
            alertAudioSource.maxDistance = 25f;
        }

        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();
        if (visionBox == null)
        {
            BoxCollider[] childBoxColliders = GetComponentsInChildren<BoxCollider>(true);
            foreach (BoxCollider childBoxCollider in childBoxColliders)
            {
                if (childBoxCollider.name == "VisionCube")
                {
                    visionBox = childBoxCollider;
                    break;
                }
            }

            foreach (BoxCollider childBoxCollider in childBoxColliders)
            {
                if (visionBox == null && childBoxCollider.isTrigger)
                {
                    visionBox = childBoxCollider;
                    break;
                }
            }
        }

        navMeshAgent.stoppingDistance = 1f; //so it doesn't get too close to the player OR the patrol points.

        patrolPointsArray = new Transform[patrolPoints.childCount]; // CANNOT use GetComponentsInChildren, because that also contains the parent.

        StartCoroutine(enemyMovement());
        keepSuspicionTimer = keepSuspicionTimerInit;
        timeBeforeAggro = timeBeforeAggroInit;
        aggroTimer = aggroTimerInit;
    }



    IEnumerator enemyMovement()
    {
        Vector3 lastSeenPlayerPosition = default;
        bool hasLastSeenPlayerPosition = false;

        for (int i = 0; i < patrolPointsArray.Length;)
        {
            yield return null; //run once per frame, so Time.deltaTime matches the time since the last run and the timers stay accurate
            patrolPointsArray[i] = patrolPoints.GetChild(i); //fill the array with the children

            // walk fast when aggro
            if (isAggro)
                navMeshAgent.speed = alertedMoveSpeed;
            else
                navMeshAgent.speed = passiveMoveSpeed;

            int visiblePoints = countVisibleDetectionPoints();
            bool seenInVisionBox = isPlayerInVisionBox() && hasLineOfSightToPlayer();
            bool seesPlayer = visiblePoints > 0 || seenInVisionBox;

            //enough of the body is visible (or the player is in the vision box) = instantly aggro, otherwise the normal suspicion route
            bool instantAggro = seenInVisionBox || visiblePoints >= pointsNeededForInstantAggro;

            if (seesPlayer)
            {
                lastSeenPlayerPosition = player.position;
                hasLastSeenPlayerPosition = true;
            }


            if (heardPlayer)
            {
                lastSeenPlayerPosition = lastHeardPlayerPosition;
                hasLastSeenPlayerPosition = true;
                heardPlayer = false; //needs this to disable it again
                keepsSuspiciousForABit = true;
                keepSuspicionTimer = keepSuspicionTimerInit;
            }


            if (patrolPointsArray[i] != null) //just a check
            {
                if ((seesPlayer || keepsSuspiciousForABit) && hasLastSeenPlayerPosition) //sees a player, is suspicious
                {
                    keepsSuspiciousForABit = true;


                    //if enough detection points are visible, or the player is in the vision box with a clear line of sight, INSTANTLY becomes AGGRO
                    if (instantAggro)
                    {
                        timeBeforeAggro = 0;
                        isAggro = true;
                    }


                    navMeshAgent.SetDestination(lastSeenPlayerPosition); // walk to the last seen/heard player location

                    if (seesPlayer)//if still spotting the player for a period of time
                    {
                        aggroTimer = aggroTimerInit;

                        timeBeforeAggro -= Time.deltaTime; //slowly becomes aggro (fully aggro at 0) while spotting player
                        if (timeBeforeAggro <= 0) // IS AGGRO NOW.
                        {
                            isAggro = true;
                            navMeshAgent.speed = alertedMoveSpeed;
                            keepSuspicionTimer = keepSuspicionTimerInit;
                        }
                    }
                    else
                    {
                        //lose suspicion when it has reached the location of the spotted player
                        if (isAggro)
                        {
                            aggroTimer = Mathf.Max(0f, aggroTimer - Time.deltaTime);
                            if (aggroTimer <= 0f)
                            {
                                isAggro = false;
                                navMeshAgent.speed = passiveMoveSpeed;
                            }
                            keepsSuspiciousForABit = true;
                            keepSuspicionTimer = keepSuspicionTimerInit; //reset the sus timer so it stays sus when ending aggro


                        }
                        else if ((navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending) || keepSuspicionTimer <= 0)
                        {
                            keepsSuspiciousForABit = false;
                        }
                        else //while still suspicious
                        {
                            keepsSuspiciousForABit = true;
                            keepSuspicionTimer -= Time.deltaTime; //slowly lose suspicion
                        }
                    }
                }
                else
                {
                    isAggro = false;

                    //reset the timers
                    keepSuspicionTimer = keepSuspicionTimerInit;
                    timeBeforeAggro = timeBeforeAggroInit;

                    navMeshAgent.SetDestination(patrolPointsArray[i].position);

                    if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
                    {
                        const float patrolPauseDuration = 2f;
                        bool wasUpdatingRotation = navMeshAgent.updateRotation;
                        navMeshAgent.isStopped = true;
                        navMeshAgent.updateRotation = false;

                        Quaternion startRotation = transform.rotation;
                        Quaternion targetRotation = patrolPointsArray[i].rotation;
                        float elapsed = 0f;
                        while (elapsed < patrolPauseDuration)
                        {
                            elapsed += Time.deltaTime;
                            transform.rotation = Quaternion.Slerp(
                                startRotation,
                                targetRotation,
                                Mathf.Clamp01(elapsed / patrolPauseDuration));
                            yield return null;
                        }

                        transform.rotation = targetRotation;
                        navMeshAgent.updateRotation = wasUpdatingRotation;
                        navMeshAgent.isStopped = false;
                        i++;
                        if (i >= patrolPointsArray.Length)
                        {
                            i = 0; //loop back to the first point
                        }
                    }
                }
            }
        }
    }


    public void KillEnemy()
    {
        Destroy(gameObject);
        Debug.Log("Enemy killed");
    }

    public bool IsPlayerDetected => isSeeingPlayer() || (isPlayerInVisionBox() && hasLineOfSightToPlayer());


    private bool isPlayerInVisionBox() //detects if the player is in the (side) vision box
    {
        if (visionBox == null || !visionBox.enabled || player == null)
            return false;

        if (cachedPlayer != player)
        {
            cachedPlayer = player;
            playerColliders = player.GetComponentsInChildren<Collider>(true);
        }

        Bounds visionBoxBounds = visionBox.bounds;
        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider != null &&
                playerCollider.enabled &&
                playerCollider.gameObject.activeInHierarchy &&
                visionBoxBounds.Intersects(playerCollider.bounds))
            {
                return true;
            }
        }

        return false;
    }

    bool isSeeingPlayer()
    {
        return countVisibleDetectionPoints() > 0;
    }

    private Transform getDetectionPoints() //finds the DetectionPoints object on the current player
    {
        if (player == null)
            return null;

        //the player can change (e.g. through hearing), so look the points up again for a new player
        if (detectionPointsOwner != player)
        {
            detectionPointsOwner = player;

            if (detectionPoints == null || !detectionPoints.IsChildOf(player))
            {
                detectionPoints = null;
                foreach (Transform child in player.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "DetectionPoints")
                    {
                        detectionPoints = child;
                        break;
                    }
                }

                if (detectionPoints == null)
                    Debug.LogWarning("No 'DetectionPoints' object found on the player, falling back to the center line of sight check.", this);
            }
        }

        return detectionPoints;
    }

    private int countVisibleDetectionPoints() //how many body points the enemy can see inside its vision cone
    {
        if (player == null || eyePoint == null)
            return 0;

        Transform points = getDetectionPoints();
        if (points == null || points.childCount == 0)
            return isPointInViewCone(getPlayerTarget()) && hasLineOfSightToPlayer() ? 1 : 0; //fallback: only the center, never instant aggro

        int visibleCount = 0;
        foreach (Transform point in points)
        {
            if (!point.gameObject.activeInHierarchy)
                continue;

            if (isPointInViewCone(point.position) && hasClearRayTo(point.position))
                visibleCount++;
        }

        return visibleCount;
    }

    private bool isPointInViewCone(Vector3 point)
    {
        Vector3 directionToPoint = point - eyePoint.position;

        if (directionToPoint.sqrMagnitude > math.square(viewDistance)) //too far away? stop.
            return false;

        float angle = Vector3.Angle(eyePoint.forward, directionToPoint); //angle between eyepoint and the point

        return angle <= viewAngle / 2f; //outside the view angle? not visible.
    }

    private bool hasClearRayTo(Vector3 point) //true if no level geometry is between the eye point and the point
    {
        Vector3 directionToPoint = point - eyePoint.position;

        int sightMask = Physics.DefaultRaycastLayers & ~(1 << gameObject.layer);
        if (Physics.Raycast(
                eyePoint.position,
                directionToPoint.normalized,
                out RaycastHit hit,
                directionToPoint.magnitude,
                sightMask,
                QueryTriggerInteraction.Ignore))
        {
            //hitting the player's own body on the way to the point still counts as seeing it
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return true; //nothing in the way
    }

    private void OnDrawGizmosSelected() //green = visible point, red = blocked/outside the cone
    {
        if (!Application.isPlaying || player == null || eyePoint == null)
            return;

        Transform points = getDetectionPoints();
        if (points == null)
            return;

        foreach (Transform point in points)
        {
            bool visible = isPointInViewCone(point.position) && hasClearRayTo(point.position);
            Gizmos.color = visible ? Color.green : Color.red;
            Gizmos.DrawLine(eyePoint.position, point.position);
        }
    }

    private Vector3 getPlayerTarget() //use the center of the player if there is a controller
    {
        var controller = player.GetComponent<UnityEngine.CharacterController>();
        return controller != null ? controller.bounds.center : player.position;
    }

    private bool hasLineOfSightToPlayer() //true if nothing blocks the ray from the eye point to the player
    {
        if (player == null || eyePoint == null)
            return false;

        Vector3 directionToPlayer = getPlayerTarget() - eyePoint.position;

        int sightMask = Physics.DefaultRaycastLayers & ~(1 << gameObject.layer);
        if (Physics.Raycast(
                eyePoint.position,
                directionToPlayer.normalized,
                out RaycastHit hit,
                directionToPlayer.magnitude,
                sightMask,
                QueryTriggerInteraction.Ignore))
        {
            bool hitPlayer = hit.transform == player.transform || hit.transform.IsChildOf(player);
            bool isOnPlayerLayer = (playerDetectMask.value & (1 << hit.transform.gameObject.layer)) != 0;
            return hitPlayer && isOnPlayerLayer;
        }

        return false;
    }
}
