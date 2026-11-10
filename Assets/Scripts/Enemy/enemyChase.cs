using System;
using System.Collections;
using System.Threading;
using CharacterController;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;


public class enemyChase : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public Transform eyePoint;
    public FirstPersonController firstPersonController;

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
    [Tooltip("Played once when the enemy loses all aggro/suspicion and goes back to patrolling.")]
    [SerializeField] private AudioClip lostSuspicionSound;
    [SerializeField, Range(0f, 1f)] private float suspiciousVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float aggroVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float lostSuspicionVolume = 1f;
    private bool wasAggro = false;
    private bool wasSuspicious = false;

    [Header("Vision Settings")]
    public float viewDistance = 10f;
    [Range(0f, 180f)]
    public float viewAngle = 90f;

    [Header("Detection")]
    public LayerMask playerDetectMask;
    [SerializeField] private float keepSuspicionTimerInit = 2f;
    [Tooltip("Seconds the enemy has to see the whole player (enough detection points) before becoming aggro, when the player is right in front of it (range 0).")]
    [SerializeField] private float timeToAggroAtZeroRange = 0.5f;
    [Tooltip("Seconds the enemy has to see the whole player (enough detection points) before becoming aggro, when the player is at the max view distance.")]
    [SerializeField] private float timeToAggroAtMaxRange = 3f;
    [SerializeField] private float aggroTimerInit = 5f;
    [SerializeField] private float passiveMoveSpeed = 1f;
    [SerializeField] private float alertedMoveSpeed = 3f;

    [Header("Parry")]
    [SerializeField] private float staggerTime = 5f;
    private bool isStaggered;
    private Coroutine staggerCoroutine;

    [Header("Enemy Attack")]
    [SerializeField] private float stoppingDistance = 5f; //so it doesn't get too close to the player OR the patrol points.
    [Tooltip("When not chasing, the enemy still never walks closer to the player than this (so it can't push the player).")]
    [SerializeField] private float personalSpaceDistance = 1.5f;
    private bool isHoldingNearPlayer = false;
    private Coroutine attackVisualCoroutine;
    [SerializeField] private GameObject enemyAttackVisual;
    private bool isAttacking = false;



    private void StopEnemyMovementForStagger()
    {
        if (navMeshAgent == null)
            return;

        navMeshAgent.ResetPath();
        navMeshAgent.velocity = Vector3.zero;
        navMeshAgent.isStopped = true;
        navMeshAgent.updateRotation = false;
    }

    private void ResumeEnemyMovementAfterStagger()
    {
        if (navMeshAgent == null)
            return;

        navMeshAgent.isStopped = false;
        navMeshAgent.updateRotation = true;
    }
    [Header("Detection Points")]
    [Tooltip("Parent object on the player whose children are the body points. Auto-found as a child of the player named 'DetectionPoints' if left empty.")]
    [SerializeField] private Transform detectionPoints;
    [Tooltip("The enemy only builds up aggro while it sees at least this many detection points. Fewer = it can only get suspicious (unless the player is in the vision box, that is always instant aggro).")]
    [SerializeField] private int pointsNeededForAggro = 4;
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
    float aggroBuildUp = 0f; //0 = not aggro at all, 1 = aggro now
    float aggroTimer = 5f;

    bool isAggro = false;
    bool isSuspicious = false;

    private bool heardPlayer = false;
    private Vector3 lastHeardPlayerPosition;





    //when detected form the sphere, this function is called by the sphere
    public void isHearingPlayer(Transform playerTransform)
    {
        Debug.Log("HEARING HEARING HEARING");
        player = playerTransform;

        //already investigating (suspicious or aggro)? then ignore new sounds, so it walks to where it
        //first heard/last saw the player instead of tracking the player's live position through walls.
        //once it reaches that spot and calms down, it can hear the player again.
        if (keepsSuspiciousForABit || isAggro)
            return;

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

    private void StartAttackVisual()
    {
        if (isAttacking == false)
        {
            attackVisualCoroutine = StartCoroutine(AttackVisualSequence());
        }

    }

    private IEnumerator AttackVisualSequence()
    {
        isAttacking = true;
        enemyAttackVisual.SetActive(true);
        enemyAttackVisual.GetComponent<Renderer>().material.color = Color.blue;
        yield return new WaitForSeconds(0.4f);
        enemyAttackVisual.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f);
        yield return new WaitForSeconds(0.4f);
        enemyAttackVisual.GetComponent<Renderer>().material.color = Color.red;

        yield return new WaitForSeconds(0.02f);
        firstPersonController.HitByEnemyAttack(this);
        yield return new WaitForSeconds(0.02f);

        enemyAttackVisual.GetComponent<Renderer>().material.color = Color.green;
        yield return new WaitForSeconds(1.5f);
        enemyAttackVisual.SetActive(false);

        attackVisualCoroutine = null;
        isAttacking = false;




    }

    //plays a sound once when the enemy enters the suspicious or aggro state, or calms down completely
    private void PlayAlertSounds()
    {
        bool suspiciousNow = keepsSuspiciousForABit && !isAggro;
        bool calmNow = !isAggro && !keepsSuspiciousForABit;

        if (isAggro && !wasAggro)
        {
            PlayAlertClip(aggroSound, aggroVolume);
        }
        else if (suspiciousNow && !wasSuspicious && !wasAggro) //not when calming down from aggro to suspicious
        {
            PlayAlertClip(suspiciousSound, suspiciousVolume);
        }
        else if (calmNow && (wasAggro || wasSuspicious)) //lost all aggro/suspicion, back to patrolling
        {
            PlayAlertClip(lostSuspicionSound, lostSuspicionVolume);
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

        const float killRange = 0.5f;
        const float killRayRadius = 0.2f; //thickness of the ray (20 cm)
        int sightMask = Physics.DefaultRaycastLayers & ~(1 << gameObject.layer);
        bool didHit = Physics.SphereCast(
            eyePoint.position,
            killRayRadius,
            eyePoint.forward,
            out RaycastHit hit,
            killRange,
            sightMask,
            QueryTriggerInteraction.Ignore);

        DebugRay.DrawSphereCastResult(eyePoint.position, eyePoint.forward, killRayRadius, killRange, didHit, hit);

        if (didHit &&
            (hit.transform == player || hit.transform.IsChildOf(player)) &&
            (playerDetectMask.value & (1 << hit.transform.gameObject.layer)) != 0)
        {

            StartAttackVisual();
        }
    }

    public void PlayerDies()
    {
        SaveSystem.ResetLevel();
    }

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.stoppingDistance = stoppingDistance; //set the stopping distance to the serialized value

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

        

        patrolPointsArray = new Transform[patrolPoints.childCount]; // CANNOT use GetComponentsInChildren, because that also contains the parent.

        StartCoroutine(enemyMovement());
        keepSuspicionTimer = keepSuspicionTimerInit;
        aggroBuildUp = 0f;
        aggroTimer = aggroTimerInit;
    }



    IEnumerator enemyMovement()
    {
        Vector3 lastSeenPlayerPosition = default;
        bool hasLastSeenPlayerPosition = false;

        for (int i = 0; i < patrolPointsArray.Length;)
        {
            if (isStaggered)
            {
                yield return null;
                continue;
            }

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

            //only builds up aggro when it sees enough of the body, otherwise it can only get suspicious
            bool seesWholePlayer = visiblePoints >= pointsNeededForAggro;

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
                    navMeshAgent.SetDestination(lastSeenPlayerPosition); // walk to the last seen/heard player location

                    if (seesPlayer)//if still spotting the player for a period of time
                    {
                        aggroTimer = aggroTimerInit;

                        //slowly becomes aggro while seeing the whole player: faster up close, slower far away
                        if (seesWholePlayer)
                            aggroBuildUp += Time.deltaTime / getTimeToAggro();

                        //player in the vision box with a clear line of sight = INSTANTLY aggro
                        if (seenInVisionBox)
                            aggroBuildUp = 1f;

                        if (aggroBuildUp >= 1f) // IS AGGRO NOW.
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
                        else
                        {
                            keepsSuspiciousForABit = true;
                            keepSuspicionTimer -= Time.deltaTime; //slowly lose suspicion
                        }
                    }
                }
                else
                {
                    isAggro = false;
                    keepSuspicionTimer = keepSuspicionTimerInit;
                    aggroBuildUp = 0f;

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
                            if (isStaggered)
                            {
                                navMeshAgent.updateRotation = wasUpdatingRotation;
                                navMeshAgent.isStopped = false;
                                yield return null;
                                continue;
                            }

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

            //the NavMeshAgent ignores the player's collider, so check the real distance to the player ourselves
            HoldPositionNearPlayer(isAggro || seesPlayer);
        }
    }

    //how many seconds of seeing the whole player it takes to become aggro: timeToAggroAtZeroRange up close, timeToAggroAtMaxRange at the view distance
    private float getTimeToAggro()
    {
        float distance = Vector3.Distance(eyePoint.position, getPlayerTarget());
        float t = viewDistance > 0f ? Mathf.Clamp01(distance / viewDistance) : 1f;
        return Mathf.Max(0.01f, Mathf.Lerp(timeToAggroAtZeroRange, timeToAggroAtMaxRange, t)); //never 0, to avoid dividing by 0
    }

    //stops the enemy when it's close enough to the player, so it never walks into (and pushes) the player
    private void HoldPositionNearPlayer(bool isChasing)
    {
        if (player == null)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distanceToPlayer = toPlayer.magnitude;

        //while chasing: stop at the stopping distance. otherwise (patrolling/searching): only stop when it would bump into the player
        float holdDistance = isChasing ? stoppingDistance : personalSpaceDistance;
        bool shouldHold = distanceToPlayer <= holdDistance;

        if (shouldHold)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero; //stop instantly instead of sliding into the player
            isHoldingNearPlayer = true;

            //the agent doesn't rotate while stopped, so keep facing the player manually
            if (isChasing && toPlayer.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(toPlayer),
                    navMeshAgent.angularSpeed * Time.deltaTime);
            }
        }
        else if (isHoldingNearPlayer) //only release the stop we set ourselves
        {
            navMeshAgent.isStopped = false;
            isHoldingNearPlayer = false;
        }
    }


    public void KillEnemy()
    {
        Destroy(gameObject);
        Debug.Log("Enemy killed");
    }

    public bool IsPlayerDetected => isSeeingPlayer() || (isPlayerInVisionBox() && hasLineOfSightToPlayer());
    public bool IsStaggered => isStaggered;


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


    public void StaggerByPlayerParry()
    {
        if (isStaggered)
            return;

        isStaggered = true;

        if (staggerCoroutine != null)
            StopCoroutine(staggerCoroutine);

        StopEnemyMovementForStagger();
        staggerCoroutine = StartCoroutine(RecoverFromStagger());
    }

    private IEnumerator RecoverFromStagger()
    {
        yield return new WaitForSeconds(staggerTime);

        isStaggered = false;
        ResumeEnemyMovementAfterStagger();
        staggerCoroutine = null;
    }

}
