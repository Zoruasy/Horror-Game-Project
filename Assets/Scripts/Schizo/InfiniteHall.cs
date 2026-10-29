using System.Collections.Generic;
using UnityEngine;

public class InfiniteHall : MonoBehaviour
{
    [Header("References")]
    public Transform speler;
    public GameObject segmentPrefab;

    [Header("Hall")]
    public float segmentLength = 4f;        // lengte van je prefab bij schaal 1
    public float zichtVooruit = 40f;        // hoe ver vooruit segmenten bestaan
    public float zichtAchteruit = 15f;      // hoe ver achteruit segmenten blijven

    [Header("Uitrekken")]
    public bool uitrekken = true;
    public float uitrekPerSegment = 0.03f;  // elk nieuw segment is iets langer
    public float maxUitrek = 3f;

    struct Segment
    {
        public GameObject obj;
        public float eindAfstand;
    }

    readonly Queue<Segment> segments = new Queue<Segment>();
    float volgendeStart = 0f;   // afstand langs de gang waar het volgende segment begint
    int teller = 0;

    void Start()
    {
        // Vul de gang in het begin
        Vulbij();
    }

    void Update()
    {
        Vulbij();
        RuimOp();
    }

    float SpelerAfstand()
    {
        // Hoe ver is de speler langs de gang (in de richting van deze transform)
        return Vector3.Dot(speler.position - transform.position, transform.forward);
    }

    void Vulbij()
    {
        float doel = SpelerAfstand() + zichtVooruit;

        while (volgendeStart < doel)
        {
            float uitrek = 1f;
            if (uitrekken)
                uitrek = Mathf.Min(1f + teller * uitrekPerSegment, maxUitrek);

            float lengte = segmentLengte * uitrek;

            Vector3 pos = transform.position + transform.forward * volgendeStart;
            GameObject seg = Instantiate(segmentPrefab, pos, transform.rotation, transform);

            // Alleen in de lengterichting (z) schalen
            seg.transform.localScale = new Vector3(1f, 1f, uitrek);

            volgendeStart += lengte;
            segmenten.Enqueue(new Segment { obj = seg, eindAfstand = volgendeStart });
            teller++;
        }
    }

    void RuimOp()
    {
        float grens = SpelerAfstand() - zichtAchteruit;

        while (segmenten.Count > 0 && segmenten.Peek().eindAfstand < grens)
        {
            Destroy(segmenten.Dequeue().obj);
        }
    }
}
