using System;
using System.Collections;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines.ExtrusionShapes;

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

    [Header("Vision Settings")]
    public float viewDistance = 10f;
    [Range(0f, 180f)]
    public float viewAngle = 90f;

    [Header("Detection")]
    public LayerMask playerDetectMask;
    [SerializeField] private float keepSuspicionTimerInit = 2f;
    [SerializeField] private float timeBeforeAlertedInit = 2f;
    [SerializeField] private float aggroTimerInit = 5f;
    [SerializeField] private float passiveMoveSpeed = 1f;
    [SerializeField] private float alertedMoveSpeed = 3f;


    [Header("Navigation")]
    private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform patrolPoints;
    [SerializeField] private BoxCollider visionBox;

    private Transform[] patrolPointsArray; //will be filled in start, just like Daans mom
    private Transform cachedPlayer;
    private Collider[] playerColliders;
    private bool keepsSuspiciousForABit = false;
    float keepSuspicionTimer = 2f;
    float timeBeforeAlerted = 2f;
    float aggroTimer = 5f;

    bool isAggro = false;
    bool isSuspicious = false;









    void Update()
    {
        TryKillPlayerAtCloseRange();

        if (visionLight == null)
            return;

        if (isAggro)
            visionLight.color = aggroVisionColor;
        else if (keepsSuspiciousForABit)
            visionLight.color = spottedVisionColor;
        else
            visionLight.color = normalVisionColor;
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
            if (respawnPoint == null)
            {
                Debug.LogError("Enemy respawn point is not assigned.", this);
                return;
            }

            UnityEngine.CharacterController characterController = player.GetComponent<UnityEngine.CharacterController>();
            if (characterController != null)
                characterController.enabled = false;

            player.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);

            if (characterController != null)
                characterController.enabled = true;

            isAggro = false;
            keepsSuspiciousForABit = false;
            keepSuspicionTimer = keepSuspicionTimerInit;
            timeBeforeAlerted = timeBeforeAlertedInit;
            aggroTimer = aggroTimerInit;
            navMeshAgent.speed = passiveMoveSpeed;
            navMeshAgent.ResetPath();
        }
    }


    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

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

        navMeshAgent.stoppingDistance = 1f; //stopping distance for the navmesh agent, so it doesn't get too close to the player OR the patrol points.

        patrolPointsArray = new Transform[patrolPoints.childCount]; // CANNOT use "= patrolPoints.GetComponentsInChildren<Transform>();" Because apparently this also contains the parent.

        StartCoroutine(enemyMovement());
        keepSuspicionTimer = keepSuspicionTimerInit;
        timeBeforeAlerted = timeBeforeAlertedInit;
        aggroTimer = aggroTimerInit;



    }







    IEnumerator enemyMovement()
    {



        // yield break; //stop patrolling if chasing player
        // navMeshAgent.isStopped = false; to pause the enemy

        Vector3 lastSeenPlayerPosition = default;
        bool hasLastSeenPlayerPosition = false;

        for (int i = 0; i < patrolPointsArray.Length;)
        {
            yield return new WaitForSeconds(0.01f); //wait before moving to the next point.
            patrolPointsArray[i] = patrolPoints.GetChild(i); //fill the array with the children
            navMeshAgent.speed = passiveMoveSpeed;

            bool seesPlayer = IsPlayerDetected;
            if (seesPlayer)
            {
                lastSeenPlayerPosition = player.position;
                hasLastSeenPlayerPosition = true;
            }

            if (patrolPointsArray[i] != null) //just a check
            {


                if ((seesPlayer || keepsSuspiciousForABit) && hasLastSeenPlayerPosition) //sees a player, is suspicious
                {
                    // Debug.Log("I SEE YOU");
                    keepsSuspiciousForABit = true;


                    navMeshAgent.SetDestination(lastSeenPlayerPosition); // walk to the last seen player location
                    Debug.Log("aggro" + timeBeforeAlerted + "sus" + keepSuspicionTimer);




                    if (seesPlayer)//if still spotting the player for a period of time
                    {
                        aggroTimer = aggroTimerInit;

                        timeBeforeAlerted -= Time.deltaTime; //slowely becomes alerted (fully alerted at 0) while spotting player
                        if (timeBeforeAlerted <= 0) // IS ALERTED NOW.
                        {
                            Debug.Log("I WILL KILL YOU");
                            isAggro = true;
                            navMeshAgent.speed = alertedMoveSpeed;
                            keepSuspicionTimer = keepSuspicionTimerInit;
                        }


                    }
                    else
                    {
                        //lose suspicion when it has reached the location of the spotted player (when it only saw the player for a sec)
                        if (isAggro)
                        {
                            aggroTimer = Mathf.Max(0f, aggroTimer - Time.deltaTime);
                            if (aggroTimer <= 0f)
                            {
                                isAggro = false;
                                navMeshAgent.speed = passiveMoveSpeed;
                            }
                        }

                        if (isAggro)
                        {
                            keepsSuspiciousForABit = true;
                            keepSuspicionTimer = keepSuspicionTimerInit;
                        }
                        else if ((navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending) || keepSuspicionTimer <= 0)
                        {
                            keepsSuspiciousForABit = false;

                        }
                        else //while still suspicious
                        {
                            keepsSuspiciousForABit = true;

                            keepSuspicionTimer -= Time.deltaTime; //slowely lose suspicion
                                                        // walk towards last seen player locaiton
                                                        // navMeshAgent.SetDestination(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));



                        }
                    }

                }
                else
                {
                    isAggro = false;

                    //reset the timers
                    keepSuspicionTimer = keepSuspicionTimerInit;
                    timeBeforeAlerted = timeBeforeAlertedInit;
                    // Debug.Log("RESETTIMERS");


                    // Debug.Log("Patrolling to point: " + patrolPointsArray[i].name + " with stopping distance: " + navMeshAgent.stoppingDistance + " and remaining distance: " + navMeshAgent.remainingDistance);
                    navMeshAgent.SetDestination(patrolPointsArray[i].position); //when it reaches the point

                    if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
                    {
                        //  yield return new WaitUntil(() => true);
                        //wait here a little, like 1 sec

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

    public bool IsPlayerDetected => isSeeingPlayer() || isPlayerInVisionBox();

    private bool isPlayerInVisionBox()
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

    bool isSeeingPlayer() //for investigation. here u can add conditions like Hearing the players footsteps or hearing something fall
    {
        if (player == null || eyePoint == null)
            return false;

        var controller = player.GetComponent<UnityEngine.CharacterController>(); //get the controller of the player to get the center of the player, if there is no controller, just use the position of the player.
        Vector3 target = controller != null ? controller.bounds.center : player.position;
        Vector3 directionToPlayer = target - eyePoint.position;


        if (directionToPlayer.sqrMagnitude > math.square(viewDistance)) //is the dist between player and enemy bigger? stop.
        {
            return false;
        }


        float angle = Vector3.Angle(eyePoint.forward, directionToPlayer); //measures the angle between eyepoint and player!

        if (angle > viewAngle / 2f) //if the angle is bigger than the view angle, stop.
        {
            return false;
        }

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