using System.Collections.Generic;
using UnityEngine;

//put this on a trigger box (e.g. in front of a door). when an enemy walks into it,
//the enemy stops moving for a bit and a "bonk" sound plays.
[RequireComponent(typeof(Collider))]
public class enemyBonkOnDoorTrigger : MonoBehaviour
{
    [Tooltip("How long the enemy stands still after bonking into the door.")]
    [SerializeField] private float stopDuration = 1.5f;
    [Tooltip("Played on the enemy when it bonks into the door.")]
    [SerializeField] private SoundCue bonkSound;

    //an enemy can have several colliders, so count them, and only bonk when the first one enters
    private readonly Dictionary<enemyChase, int> collidersInside = new Dictionary<enemyChase, int>();

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        enemyChase enemy = other.GetComponentInParent<enemyChase>();
        if (enemy == null)
            return;

        collidersInside.TryGetValue(enemy, out int count);
        collidersInside[enemy] = count + 1;

        if (count == 0) //enemy just stepped onto the trigger
            enemy.BonkOnDoor(stopDuration, bonkSound);
    }

    private void OnTriggerExit(Collider other)
    {
        enemyChase enemy = other.GetComponentInParent<enemyChase>();
        if (enemy == null || !collidersInside.TryGetValue(enemy, out int count))
            return;

        if (count <= 1)
            collidersInside.Remove(enemy);
        else
            collidersInside[enemy] = count - 1;
    }
}
