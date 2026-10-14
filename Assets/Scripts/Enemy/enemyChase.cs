using System;
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

    void Update()
    {
        if (CanSeePlayer())
        {
            Debug.Log("YURRRRRRRRRRRRRRRRP I SEE YOU");
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z));
        }
        else
        {
            navMeshAgent.isStopped = true;
        }
    }

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    // void myRaycast()
    // {
    //     Debug.DrawRay(eyePoint.position, Vector3() * directionToPlayer.magnitude, Color.red);
    // }

    bool CanSeePlayer()
    {
        if (player == null || eyePoint == null) 
            return false;

        Vector3 directionToPlayer = player.position - eyePoint.position; //just always know the distance between player and enemy

        
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