using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maakt een dichte sneeuw-mesh en houdt een "footprint map" (RenderTexture) bij.
/// Zet dit op een leeg GameObject op grondhoogte (rotatie 0, scale 1).
/// </summary>
[ExecuteAlways] // zodat je de sneeuw ook in de editor ziet
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SnowSurface : MonoBehaviour
{
    public static SnowSurface Instance { get; private set; }

    [Header("Gebied")]
    [Tooltip("Grootte van het sneeuwveld in meters (X, Z). Midden = positie van dit object.")]
    public Vector2 size = new Vector2(20f, 20f);
    [Tooltip("Aantal vakjes per zijde. Hoger = mooiere afdrukken, maar zwaarder. 256-512 is goed.")]
    [Range(16, 1024)] public int meshResolution = 384;
    [Tooltip("Dikte van de sneeuwlaag in meters. Een volle afdruk gaat tot de grond.")]
    public float snowThickness = 0.15f;
    [Tooltip("Voeten die verder dan dit boven/onder de sneeuw zijn (andere verdieping) maken geen afdruk.")]
    public float heightTolerance = 1.0f;

    [Header("Grond volgen (voor heuvels / terrain)")]
    public bool conformToGround = false;
    [Tooltip("Alleen de grond! Niet de layer van player/enemies.")]
    public LayerMask groundLayers = ~0;
    public float raycastHeight = 50f;

    [Header("Footprints")]
    [Tooltip("Resolutie van de footprint map. 1024 op 20m = 2 cm per pixel.")]
    public int trailTextureSize = 1024;
    [Tooltip("Hoeveel seconden het duurt voor een afdruk helemaal weg is.")]
    public float fadeDuration = 30f;

    [Header("Shader")]
    [Tooltip("Sleep hier 'Hidden/SnowTrailUpdate' in (anders wordt hij in een build weggelaten).")]
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
        BuildMesh(); // preview in de editor
        ResetGlobals();
    }
#endif

    void Awake()
    {
        if (!Application.isPlaying) return;
        if (Instance != null && Instance != this)
            Debug.LogWarning("Er is meer dan één SnowSurface; alleen de laatste krijgt footprints.");
        Instance = this;

        BuildMesh();

        if (trailUpdateShader == null) trailUpdateShader = Shader.Find("Hidden/SnowTrailUpdate");
        if (trailUpdateShader == null)
        {
            Debug.LogError("SnowSurface: shader 'Hidden/SnowTrailUpdate' niet gevonden.");
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
        // RFloat: nodig omdat we elke frame een héél klein beetje aftrekken (30 sec fade)
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

        // Nieuwe voetafdrukken van deze frame klaarzetten (max 32, rest volgende frame)
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

        // Ping-pong: lees uit A, schrijf naar B, wissel om
        Graphics.Blit(trailA, trailB, updateMat);
        (trailA, trailB) = (trailB, trailA);

        PushGlobals();
    }

    /// <summary>
    /// Druk een voetafdruk in de sneeuw.
    /// </summary>
    /// <param name="worldPos">Positie van de voet (alleen X en Z tellen).</param>
    /// <param name="forward">Richting waarin de voet wijst.</param>
    /// <param name="width">Breedte van de afdruk in meters.</param>
    /// <param name="length">Lengte van de afdruk in meters.</param>
    /// <param name="depth">0..1, 1 = helemaal tot de grond.</param>
    public void AddFootprint(Vector3 worldPos, Vector3 forward, float width, float length, float depth = 1f)
    {
        Vector2 min = AreaMin;
        Vector2 uv = new Vector2((worldPos.x - min.x) / size.x, (worldPos.z - min.y) / size.y);
        if (uv.x < -0.05f || uv.x > 1.05f || uv.y < -0.05f || uv.y > 1.05f) return; // buiten de sneeuw
        if (Mathf.Abs(worldPos.y - transform.position.y) > heightTolerance) return;   // andere verdieping

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
                uvs[i] = new Vector2(fx * size.x, fz * size.y); // 1 texture-tile per meter
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
        // DontSave: de mesh wordt niet in de scene opgeslagen (scheelt MB's), hij wordt steeds opnieuw gemaakt
        var mesh = new Mesh { name = "SnowMesh", hideFlags = HideFlags.DontSave };
        generatedMesh = mesh;
        if (verts.Length > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var b = mesh.bounds;
        b.Expand(new Vector3(0f, snowThickness * 2f + 0.5f, 0f)); // ruimte voor het indeuken
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

    // Zonder dit zou de sneeuw na het stoppen van Play mode raar ingezakt blijven in de editor
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
