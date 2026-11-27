using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a dense snow mesh and maintains a "footprint map" (RenderTexture).
/// Put this on an empty GameObject at floor height (rotation 0, scale 1).
/// </summary>
[ExecuteAlways] // so you can also see the snow in the editor
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SnowSurface : MonoBehaviour
{
    public static SnowSurface Instance { get; private set; }

    [Header("Area")]
    [Tooltip("Size of the snow field in meters (X, Z). Center = position of this object.")]
    public Vector2 size = new Vector2(20f, 20f);
    [Tooltip("Number of grid cells per side. Higher = nicer footprints, but heavier. 256-512 works well.")]
    [Range(16, 1024)] public int meshResolution = 384;
    [Tooltip("Thickness of the snow layer in meters. A full-depth footprint goes all the way down to the floor.")]
    public float snowThickness = 0.15f;
    [Tooltip("Feet that are further than this above/below the snow (another floor level) leave no footprint.")]
    public float heightTolerance = 1.0f;

    [Header("Conform to ground (for hills / terrain)")]
    public bool conformToGround = false;
    [Tooltip("Ground only! Not the player/enemy layers.")]
    public LayerMask groundLayers = ~0;
    public float raycastHeight = 50f;

    [Header("Footprints")]
    [Tooltip("Resolution of the footprint map. 1024 over 20 m = 2 cm per pixel.")]
    public int trailTextureSize = 1024;
    [Tooltip("How many seconds it takes for a footprint to fully disappear.")]
    public float fadeDuration = 30f;

    [Header("Shader")]
    [Tooltip("Drag 'Hidden/SnowTrailUpdate' in here (otherwise it gets stripped from builds).")]
    public Shader trailUpdateShader;

    const int MaxStamps = 32;

    RenderTexture trailA, trailB;
    Material updateMat;
    readonly List<Vector4> pendingA = new List<Vector4>();
    readonly List<Vector4> pendingB = new List<Vector4>();
    readonly Vector4[] stampA = new Vector4[MaxStamps];
    readonly Vector4[] stampB = new Vector4[MaxStamps];

    static readonly int TrailTexId   = Shader.PropertyToID("_SnowTrailTex");
    static readonly int SnowAreaId   = Shader.PropertyToID("_SnowArea");
    static readonly int SnowParamsId = Shader.PropertyToID("_SnowParams");
    static readonly int FadeId       = Shader.PropertyToID("_FadeAmount");
    static readonly int CountId      = Shader.PropertyToID("_StampCount");
    static readonly int AreaSizeId   = Shader.PropertyToID("_AreaSize");
    static readonly int StampAId     = Shader.PropertyToID("_StampA");
    static readonly int StampBId     = Shader.PropertyToID("_StampB");

    Vector2 AreaMin => new Vector2(transform.position.x - size.x * 0.5f,
                                   transform.position.z - size.y * 0.5f);

    Mesh generatedMesh;

#if UNITY_EDITOR
    void OnEnable()
    {
        if (Application.isPlaying) return;
        BuildMesh(); // preview in the editor
        ResetGlobals();
    }
#endif

    void Awake()
    {
        if (!Application.isPlaying) return;
        if (Instance != null && Instance != this)
            Debug.LogWarning("There is more than one SnowSurface; only the last one receives footprints.");
        Instance = this;

        BuildMesh();

        if (trailUpdateShader == null) trailUpdateShader = Shader.Find("Hidden/SnowTrailUpdate");
        if (trailUpdateShader == null)
        {
            Debug.LogError("SnowSurface: shader 'Hidden/SnowTrailUpdate' not found.");
            enabled = false;
            return;
        }
        updateMat = new Material(trailUpdateShader);

        trailA = CreateTrailTexture();
        trailB = CreateTrailTexture();
        PushGlobals();
    }

    RenderTexture CreateTrailTexture()
    {
        // RFloat: needed because we subtract a very small amount every frame (30 sec fade)
        var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat)
            ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf;
        var rt = new RenderTexture(trailTextureSize, trailTextureSize, 0, format)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "SnowTrail"
        };
        rt.Create();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = prev;
        return rt;
    }

    void PushGlobals()
    {
        Vector2 min = AreaMin;
        Shader.SetGlobalVector(SnowAreaId, new Vector4(min.x, min.y, size.x, size.y));
        Shader.SetGlobalVector(SnowParamsId, new Vector4(1f / trailTextureSize, snowThickness, 0, 0));
        Shader.SetGlobalTexture(TrailTexId, trailA);
    }

    void Update()
    {
        if (!Application.isPlaying || updateMat == null) return;

        // Queue this frame's new footprints (max 32, the rest go next frame)
        int count = Mathf.Min(pendingA.Count, MaxStamps);
        for (int i = 0; i < MaxStamps; i++)
        {
            stampA[i] = i < count ? pendingA[i] : Vector4.zero;
            stampB[i] = i < count ? pendingB[i] : Vector4.zero;
        }
        pendingA.RemoveRange(0, count);
        pendingB.RemoveRange(0, count);

        float fade = fadeDuration > 0f ? Time.deltaTime / fadeDuration : 1f;
        updateMat.SetFloat(FadeId, fade);
        updateMat.SetFloat(CountId, count);
        updateMat.SetVector(AreaSizeId, new Vector4(size.x, size.y, 0, 0));
        updateMat.SetVectorArray(StampAId, stampA);
        updateMat.SetVectorArray(StampBId, stampB);

        // Ping-pong: read from A, write to B, then swap
        Graphics.Blit(trailA, trailB, updateMat);
        (trailA, trailB) = (trailB, trailA);

        PushGlobals();
    }

    /// <summary>
    /// Press a footprint into the snow.
    /// </summary>
    /// <param name="worldPos">Position of the foot (only X and Z are used).</param>
    /// <param name="forward">Direction the foot is pointing.</param>
    /// <param name="width">Width of the footprint in meters.</param>
    /// <param name="length">Length of the footprint in meters.</param>
    /// <param name="depth">0..1, 1 = all the way down to the floor.</param>
    public void AddFootprint(Vector3 worldPos, Vector3 forward, float width, float length, float depth = 1f)
    {
        Vector2 min = AreaMin;
        Vector2 uv = new Vector2((worldPos.x - min.x) / size.x, (worldPos.z - min.y) / size.y);
        if (uv.x < -0.05f || uv.x > 1.05f || uv.y < -0.05f || uv.y > 1.05f) return; // outside the snow
        if (Mathf.Abs(worldPos.y - transform.position.y) > heightTolerance) return;   // different floor level

        Vector2 f = new Vector2(forward.x, forward.z);
        if (f.sqrMagnitude < 0.0001f) f = Vector2.up;
        f.Normalize();

        pendingA.Add(new Vector4(uv.x, uv.y, width * 0.5f, length * 0.5f));
        pendingB.Add(new Vector4(f.x, f.y, Mathf.Clamp01(depth), 0));
    }

    [ContextMenu("Rebuild Mesh")]
    public void BuildMesh()
    {
        int res = Mathf.Max(1, meshResolution);
        int vx = res + 1;
        var verts = new Vector3[vx * vx];
        var uvs = new Vector2[vx * vx];
        var tris = new int[res * res * 6];

        for (int z = 0; z < vx; z++)
        {
            for (int x = 0; x < vx; x++)
            {
                float fx = (float)x / res;
                float fz = (float)z / res;
                var local = new Vector3((fx - 0.5f) * size.x, snowThickness, (fz - 0.5f) * size.y);

                if (conformToGround)
                {
                    Vector3 origin = transform.TransformPoint(new Vector3(local.x, 0f, local.z)) + Vector3.up * raycastHeight;
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2f,
                                        groundLayers, QueryTriggerInteraction.Ignore))
                    {
                        local.y = transform.InverseTransformPoint(hit.point).y + snowThickness;
                    }
                }

                int i = z * vx + x;
                verts[i] = local;
                uvs[i] = new Vector2(fx * size.x, fz * size.y); // 1 texture tile per meter
            }
        }

        int t = 0;
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                int i0 = z * vx + x, i1 = i0 + 1, i2 = i0 + vx, i3 = i2 + 1;
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                tris[t++] = i1; tris[t++] = i2; tris[t++] = i3;
            }
        }

        if (generatedMesh != null)
        {
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
        // DontSave: the mesh is not saved into the scene (saves megabytes); it is rebuilt every time
        var mesh = new Mesh { name = "SnowMesh", hideFlags = HideFlags.DontSave };
        generatedMesh = mesh;
        if (verts.Length > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var b = mesh.bounds;
        b.Expand(new Vector3(0f, snowThickness * 2f + 0.5f, 0f)); // room for the snow to be pressed down
        mesh.bounds = b;

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (trailA) trailA.Release();
        if (trailB) trailB.Release();
        if (updateMat) Destroy(updateMat);
        if (generatedMesh)
        {
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
        ResetGlobals();
    }

    // Without this the snow would stay oddly sunken in the editor after leaving Play mode
    static void ResetGlobals()
    {
        Shader.SetGlobalTexture(TrailTexId, Texture2D.blackTexture);
        Shader.SetGlobalVector(SnowAreaId, Vector4.zero);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * snowThickness * 0.5f,
                            new Vector3(size.x, snowThickness, size.y));
    }
}
