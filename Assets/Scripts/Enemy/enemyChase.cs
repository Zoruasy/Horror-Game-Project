using System;
using System.Collections;
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

    [Header("Navigation")]
    private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform patrolPoints;

    private Transform[] patrolPointsArray; //will be filled in start, just like Daans mom










    void Update()
    {




    }

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        navMeshAgent.stoppingDistance = 0.5f; //stopping distance for the navmesh agent, so it doesn't get too close to the player OR the patrol points.

        patrolPointsArray = new Transform[patrolPoints.childCount]; // CANNOT use "= patrolPoints.GetComponentsInChildren<Transform>();" Because apparently this also contains the parent.

        StartCoroutine(Patrolling());



    }







    IEnumerator Patrolling()
    {

        if (isChasingPlayer())
        {
            // yield break; //stop patrolling if chasing player
            // navMeshAgent.isStopped = false; to pause the enemy

            Debug.Log("YURRRRRRRRRRRRRRRRP I SEE YOU");
            yield return new WaitForSeconds(0.1f); //update speed
            navMeshAgent.SetDestination(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));


        }
        else
        {




            for (int i = 0; i < patrolPointsArray.Length;)
            {
                yield return new WaitForSeconds(0.1f); //wait before moving to the next point.
                patrolPointsArray[i] = patrolPoints.GetChild(i); //fill the array with the children



                if (patrolPointsArray[i] != null)
                {
                    Debug.Log("Patrolling to point: " + patrolPointsArray[i].name + " with stopping distance: " + navMeshAgent.stoppingDistance + " and remaining distance: " + navMeshAgent.remainingDistance);
                    navMeshAgent.SetDestination(patrolPointsArray[i].position); //when it reaches the point

                    if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && !navMeshAgent.pathPending)
                    {
                        //  yield return new WaitUntil(() => true);
                        i++;
                        if (i >= patrolPointsArray.Length)
                        {
                            i = 0; //loop back to the first point
                        }

                    }
                    else
                    {


                    }

                }
            }
        }
    }



    bool isChasingPlayer() //for chasing 
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