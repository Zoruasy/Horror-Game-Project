using UnityEngine;

public class Inspectable : Interactable
{
    [Tooltip("Turns the object to Inspect Rotation (relative to the camera) when picked up, instead of keeping its orientation.")]
    [SerializeField] bool faceCamera;
    [SerializeField] Vector3 inspectRotation;
    [Tooltip("Size on screen while inspected, relative to the inspector's Screen Fill.")]
    [SerializeField, Range(0.25f, 2f)] float inspectSize = 1f;

    public virtual bool FaceCamera => faceCamera;
    public virtual Vector3 InspectRotation => inspectRotation;
    public float InspectSize => inspectSize;

    void Reset() => prompt = "Inspect";

    public override bool CanInteract(GameObject interactor) => ObjectInspector.Instance && !ObjectInspector.Instance.IsOpen;

    public override void Interact(GameObject interactor)
    {
        if (ObjectInspector.Instance) ObjectInspector.Instance.Inspect(this, interactor);
    }

    public Bounds GetLocalBounds()
    {
        bool found = false;
        var bounds = new Bounds();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            Encapsulate(ref bounds, ref found, r.transform, r.localBounds);
        }
        if (found) return bounds;

        foreach (Collider c in GetComponentsInChildren<Collider>())
            Encapsulate(ref bounds, ref found, null, c.bounds);
        return found ? bounds : new Bounds(Vector3.zero, Vector3.one * 0.2f);
    }

    void Encapsulate(ref Bounds bounds, ref bool found, Transform space, Bounds box)
    {
        Vector3 min = box.min, max = box.max;
        foreach (float x in new[] { min.x, max.x })
        foreach (float y in new[] { min.y, max.y })
        foreach (float z in new[] { min.z, max.z })
        {
            var corner = new Vector3(x, y, z);
            if (space) corner = space.TransformPoint(corner);
            corner = transform.InverseTransformPoint(corner);

            if (found) bounds.Encapsulate(corner);
            else bounds = new Bounds(corner, Vector3.zero);
            found = true;
        }
    }
}
