
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SplineWall : MonoBehaviour
{
    public SplineContainer splineContainer;

    public float height = 3f;
    public float thickness = 0.2f;
    public float segmentsPerMeter = 4f;

    private Mesh generatedMesh;

    void OnEnable()
    {
        GenerateWall();
    }

    void OnValidate()
    {
        GenerateWall();
    }

    void Update()
    {
        if (!Application.isPlaying)
            GenerateWall();
    }

    void GenerateWall()
    {
        if (splineContainer == null ||
            splineContainer.Splines.Count == 0)
            return;

        var spline = splineContainer.Spline;
        float length = spline.GetLength();

        int segments = Mathf.Max(
            2, Mathf.CeilToInt(length * segmentsPerMeter));

        bool closed = spline.Closed;
        int rings = closed ? segments : segments + 1;

        Vector3[] vertices = new Vector3[rings * 4];
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i < rings; i++)
        {
            float t = (float)i / segments;

            float3 p = spline.EvaluatePosition(t);
            float3 tangent = spline.EvaluateTangent(t);

            Vector3 worldPos =
                splineContainer.transform.TransformPoint((Vector3)p);

            Vector3 worldTangent =
                splineContainer.transform.TransformDirection(
                    (Vector3)tangent).normalized;

            Vector3 side = Vector3.Cross(
                Vector3.up, worldTangent).normalized;

            if (side.sqrMagnitude < 0.001f)
                side = Vector3.right;

            Vector3 center =
                transform.InverseTransformPoint(worldPos);

            Vector3 offset =
                transform.InverseTransformVector(
                    side * thickness * 0.5f);

            Vector3 up =
                transform.InverseTransformVector(
                    Vector3.up * height);

            int v = i * 4;

            vertices[v] = center - offset;
            vertices[v + 1] = center + offset;
            vertices[v + 2] = center - offset + up;
            vertices[v + 3] = center + offset + up;

            float u = t * length;

            uv[v] = new Vector2(u, 0);
            uv[v + 1] = new Vector2(u, 1);
            uv[v + 2] = new Vector2(u, 0);
            uv[v + 3] = new Vector2(u, 1);
        }

        int connections = closed ? rings : rings - 1;
        int[] triangles = new int[connections * 24];

        int index = 0;

        for (int i = 0; i < connections; i++)
        {
            int a = i * 4;
            int b = ((i + 1) % rings) * 4;

            AddQuad(a, b, a + 2, b + 2);
            AddQuad(b + 1, a + 1, b + 3, a + 3);
            AddQuad(a + 2, b + 2, a + 3, b + 3);
            AddQuad(b, a, b + 1, a + 1);
        }

        if (generatedMesh != null)
        {
            if (Application.isPlaying)
                Destroy(generatedMesh);
            else
                DestroyImmediate(generatedMesh);
        }

        generatedMesh = new Mesh();
        generatedMesh.name = "Generated Spline Wall";

        if (vertices.Length > 65535)
            generatedMesh.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;

        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
        generatedMesh.uv = uv;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = generatedMesh;

        void AddQuad(int a, int b, int c, int d)
        {
            triangles[index++] = a;
            triangles[index++] = c;
            triangles[index++] = b;

            triangles[index++] = b;
            triangles[index++] = c;
            triangles[index++] = d;
        }
    }
}
