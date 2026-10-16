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

    [Header("Vision Settings")]
    public float viewDistance = 10f;
    [Range(0f, 180f)]
    public float viewAngle = 90f;

    [Header("Detection")]
    public LayerMask playerDetectMask;
    [SerializeField] private float keepSuspicionTimerInit = 2f;
    [SerializeField] private float timeBeforeAlertedInit = 2f;
    [SerializeField] private float passiveMoveSpeed = 1f;
    [SerializeField] private float alertedMoveSpeed = 3f;


    [Header("Navigation")]
    private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform patrolPoints;

    private Transform[] patrolPointsArray; //will be filled in start, just like Daans mom
    private bool keepsSuspiciousForABit = false;
    float keepSuspicionTimer = 2f;
    float timeBeforeAlerted = 2f;









    void Update()
    {




    }

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        navMeshAgent.stoppingDistance = 1f; //stopping distance for the navmesh agent, so it doesn't get too close to the player OR the patrol points.

        patrolPointsArray = new Transform[patrolPoints.childCount]; // CANNOT use "= patrolPoints.GetComponentsInChildren<Transform>();" Because apparently this also contains the parent.

        StartCoroutine(enemyMovement());
        keepSuspicionTimer = keepSuspicionTimerInit;
        timeBeforeAlerted = timeBeforeAlertedInit;



    }







    IEnumerator enemyMovement()
    {



        // yield break; //stop patrolling if chasing player
        // navMeshAgent.isStopped = false; to pause the enemy

        for (int i = 0; i < patrolPointsArray.Length;)
        {
            yield return new WaitForSeconds(0.1f); //wait before moving to the next point.
            patrolPointsArray[i] = patrolPoints.GetChild(i); //fill the array with the children
            navMeshAgent.speed = passiveMoveSpeed;



            if (patrolPointsArray[i] != null) //just a check
            {



                if (isSuspicious() || keepsSuspiciousForABit) //sees a player, is suspicious
                {
                    Debug.Log("I SEE YOU");




                    if ((navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending) || keepSuspicionTimer <= 0) //lose suspicion
                    {
                        keepsSuspiciousForABit = false;

                    }
                    else //while still suspicious
                    {
                        keepsSuspiciousForABit = true;
                        if (isSuspicious())//if still spotting the player
                        {
                            Debug.Log("aggro" + timeBeforeAlerted);
                            timeBeforeAlerted -= Time.deltaTime; //slowely becomes alerted (fully alerted at 0) while spotting player
                            if (timeBeforeAlerted <= 0) // IS AGGRESSIVE NOW.
                            {
                                Debug.Log("I WILL KILL YOU");
                                navMeshAgent.speed = alertedMoveSpeed;
                                navMeshAgent.SetDestination(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));
                            }

                        }
                        else
                        {

                            keepSuspicionTimer -= Time.deltaTime; //slowely lose suspicion
                                                                  // walk towards last seen player locaiton
                            navMeshAgent.SetDestination(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));

                        }










                    }

                }
                else
                {
                    // Debug.Log("Patrolling to point: " + patrolPointsArray[i].name + " with stopping distance: " + navMeshAgent.stoppingDistance + " and remaining distance: " + navMeshAgent.remainingDistance);
                    navMeshAgent.SetDestination(patrolPointsArray[i].position); //when it reaches the point

                    if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
                    {
                        //  yield return new WaitUntil(() => true);
                        i++;
                        if (i >= patrolPointsArray.Length)
                        {
                            i = 0; //loop back to the first point
                            //reset the timers
                            keepSuspicionTimer = keepSuspicionTimerInit;
                            timeBeforeAlerted = timeBeforeAlertedInit;
                        }

                    }
                }




            }
        }

    }



    bool isSuspicious() //for investigation. here u can add conditions like Hearing the players footsteps or hearing something fall
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

        //check the layer for the player and if there is an obj in between the player and the enemy
        if (Physics.Raycast(
                eyePoint.position,
                directionToPlayer.normalized,
                out RaycastHit hit,
                directionToPlayer.magnitude,
                playerDetectMask,
                QueryTriggerInteraction.Ignore))
        {
            return hit.transform == player.transform || hit.transform.IsChildOf(player);
        }

        return false;
    }
}