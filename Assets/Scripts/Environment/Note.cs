using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class Note : Inspectable
{
    static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    static readonly int MainTex = Shader.PropertyToID("_MainTex");
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    [SerializeField] Texture2D image;
    [Tooltip("Renderer the image is projected on (a Quad). Uses this object's renderer if empty.")]
    [SerializeField] Renderer surface;
    [Tooltip("Longest side of the note in the world, in meters. The other side follows the image ratio.")]
    [SerializeField] float size = 0.3f;
    [Tooltip("Back side of the note (Quad child turned 180°), painted with Back Color.")]
    [SerializeField] Renderer back;
    [SerializeField] Color backColor = new Color(0.86f, 0.82f, 0.72f);

    public Texture2D Image => image;

    void Awake() => ApplyImage();

#if UNITY_EDITOR
    void OnValidate() => UnityEditor.EditorApplication.delayCall += () => { if (this) ApplyImage(); };
#endif

    void ApplyImage()
    {
        Renderer target = surface ? surface : GetComponent<Renderer>();
        if (!target) return;

        if (image) FitToImage(target.transform);

        var block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        if (image)
        {
            block.SetTexture(BaseMap, image);
            block.SetTexture(MainTex, image);
            block.SetColor(BaseColor, Color.white);
        }
        else block.Clear();
        target.SetPropertyBlock(block);

        if (!back) return;
        var backBlock = new MaterialPropertyBlock();
        backBlock.SetColor(BaseColor, backColor);
        back.SetPropertyBlock(backBlock);
    }

    void FitToImage(Transform target)
    {
        float aspect = (float)image.width / image.height;
        Vector3 scale = aspect >= 1f
            ? new Vector3(size, size / aspect, 1f)
            : new Vector3(size * aspect, size, 1f);
        if (target.localScale != scale) target.localScale = scale;
    }

    public override string GetPrompt(GameObject interactor) => "Read";

    public override bool FaceCamera => true;
}
