using System.Collections.Generic;
using UnityEngine;

public class InfiniteHall : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject segmentPrefab;
    public Transform backWall;               // wall that closes the hallway behind the player

    [Header("Hallway")]
    public float segmentLength = 1f;         // depth of one prefab (1 for 5x5x1)
    public float startLength = 15f;          // starting hallway length in meters
    public float heightOffset = 2.5f;        // half the prefab height (center pivot)

    [Header("Effect")]
    public float growth = 2f;                // how fast the end moves away after halfway
    public float shrink = 1.5f;              // how fast the start moves forward after halfway
    public float minBehind = 3f;             // minimum hallway kept behind the player
    public float maxLength = 80f;            // safety limit for total hallway length

    readonly Dictionary<int, GameObject> segments = new Dictionary<int, GameObject>();

    void Update()
    {
        float d = PlayerDistance();
        float half = startLength / 2f;
        float t = Mathf.Max(0f, d - half);   // how far past the halfway point

        float end = startLength + t * growth;
        float start = Mathf.Min(t * shrink, d - minBehind);
        start = Mathf.Max(0f, start);
        end = Mathf.Min(end, start + maxLength);

        int firstIndex = Mathf.FloorToInt(start / segmentLength);
        int lastIndex = Mathf.CeilToInt(end / segmentLength) - 1;

        // Spawn missing segments
        for (int i = firstIndex; i <= lastIndex; i++)
        {
            if (!segments.ContainsKey(i))
                segments[i] = SpawnSegment(i);
        }

        // Remove segments outside the range
        List<int> toRemove = null;
        foreach (var kvp in segments)
        {
            if (kvp.Key < firstIndex || kvp.Key > lastIndex)
            {
                if (toRemove == null) toRemove = new List<int>();
                toRemove.Add(kvp.Key);
            }
        }
        if (toRemove != null)
        {
            foreach (int i in toRemove)
            {
                Destroy(segments[i]);
                segments.Remove(i);
            }
        }

        // Back wall snaps to the start of the first segment
        if (backWall != null)
            backWall.position = HallPoint(firstIndex * segmentLength) + Vector3.up * heightOffset;
    }

    GameObject SpawnSegment(int index)
    {
        // Pivot is in the center: half a segment further than the start
        Vector3 pos = HallPoint((index + 0.5f) * segmentLength) + Vector3.up * heightOffset;
        return Instantiate(segmentPrefab, pos, transform.rotation, transform);
    }

    Vector3 HallPoint(float distance)
    {
        return transform.position + transform.forward * distance;
    }

    float PlayerDistance()
    {
        return Vector3.Dot(player.position - transform.position, transform.forward);
    }
}