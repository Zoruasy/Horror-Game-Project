using System.Collections.Generic;
using UnityEngine;

public class  InfiniteHall : MonoBehaviour
{
    [Header("Referenties")]
    public Transform speler;
    public GameObject segmentPrefab;

    [Header("Gang")]
    public float segmentLength = 1f;        // prefab length
    public float hallwayFront = 5f;        // how many segments in front
    public float hallwayBehind = 3f;      // how many segments behind

    [Header("Stretch")]
    public bool Stretch = true;
    public float stretchPerSegment = 0.03f;  // elk nieuw segment is iets langer
    public float maxStretch = 3f;

    struct Segment
    {
        public GameObject obj;
        public float eindAfstand;
    }

    readonly Queue<Segment> segmenten = new Queue<Segment>();
    float nextStart = 0f;   // range when next segment starts
    int counter = 0;

    void Start()
    {
        // Fill the hallway at the start
        AddSegments();
    }

    void Update()
    {
        AddSegments();
        DestroySegments();
    }

    float PlayerDistance()
    {
        // How far is the player into the hallway
        return Vector3.Dot(speler.position - transform.position, transform.forward);
    }

    void AddSegments()
    {
        float goal = PlayerDistance() + hallwayFront;

        while (nextStart < goal)
        {
            float stretch = 1f;
            if (Stretch)
                stretch = Mathf.Min(1f + counter * stretchPerSegment, maxStretch);

            float lengte = segmentLength * stretch;

            Vector3 pos = transform.position + transform.forward * nextStart;
            GameObject seg = Instantiate(segmentPrefab, pos, transform.rotation, transform);

            // Only scale in length (z) axis
            seg.transform.localScale = new Vector3(1f, 1f, stretch);

            nextStart += lengte;
            segmenten.Enqueue(new Segment { obj = seg, eindAfstand = nextStart });
            counter++;
        }
    }

    void DestroySegments()
    {
        float grens = PlayerDistance() - hallwayBehind;

        while (segmenten.Count > 0 && segmenten.Peek().eindAfstand < grens)
        {
            Destroy(segmenten.Dequeue().obj);
        }
    }
}