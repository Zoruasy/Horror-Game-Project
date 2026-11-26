using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WorldKeypadSetup
{
    const string Menu = "Tools/Keypad/Set Up Selected Keypad";

    [MenuItem(Menu)]
    public static void SetUp()
    {
        var root = Selection.activeGameObject;
        var view = SceneView.lastActiveSceneView;
        if (!root || EditorUtility.IsPersistent(root) || !view)
        {
            Debug.LogError("Select the keypad instance in the Scene view first.");
            return;
        }

        if (root.GetComponent<WorldKeypad>()) return;

        var meshes = root.GetComponentsInChildren<MeshFilter>()
            .Where(mesh => mesh.sharedMesh && Enumerable.Range(1, 12)
                .Any(index => mesh.name == $"Cube.{index:000}"))
            .ToArray();
        if (meshes.Length != 12 || meshes.Select(mesh => mesh.name).Distinct().Count() != 12)
        {
            Debug.LogError("Select the keypad FBX instance with its twelve Cube.001–Cube.012 meshes.", root);
            return;
        }

        Vector3 Center(MeshFilter mesh) => mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center);
        var rows = meshes.OrderByDescending(mesh => root.transform.InverseTransformPoint(Center(mesh)).y)
            .ChunkForKeypad();
        Vector3 up = root.transform.up;
        // Use the outermost pair even when the FBX hierarchy lists columns out of order.
        Vector3 across = rows[0].Select(mesh => Center(mesh) - Center(rows[0][0]))
            .OrderByDescending(offset => offset.sqrMagnitude).First();
        Vector3 outward = Vector3.Cross(across, up).normalized;
        Vector3 center = meshes.Select(Center).Aggregate(Vector3.zero, (sum, point) => sum + point) / 12;
        if (Vector3.Dot(outward, view.camera.transform.position - center) < 0) outward = -outward;
        Vector3 right = Vector3.Cross(outward, up);
        Quaternion rotation = Quaternion.LookRotation(-outward, up);
        float spacing = rows[0].Max(mesh => Vector3.Dot(Center(mesh), right))
            - rows[0].Min(mesh => Vector3.Dot(Center(mesh), right));
        spacing *= 0.5f;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Set Up World Keypad");
        var keypad = Undo.AddComponent<WorldKeypad>(root);
        var keys = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "OK" };

        for (int row = 0; row < 4; row++)
        {
            var ordered = rows[row].OrderBy(mesh => Vector3.Dot(Center(mesh), right)).ToArray();
            for (int column = 0; column < 3; column++)
            {
                var mesh = ordered[column];
                var bounds = mesh.sharedMesh.bounds;
                string key = keys[row * 3 + column];

                var collider = Undo.AddComponent<BoxCollider>(mesh.gameObject);
                Undo.RecordObject(collider, "Fit keypad collider");
                collider.center = bounds.center;
                collider.size = bounds.size;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);

                var button = new SerializedObject(Undo.AddComponent<WorldKeypadButton>(mesh.gameObject));
                button.FindProperty("keypad").objectReferenceValue = keypad;
                button.FindProperty("key").stringValue = key;
                button.ApplyModifiedProperties();

                var localX = mesh.transform.TransformVector(Vector3.right);
                var localY = mesh.transform.TransformVector(Vector3.up);
                var localZ = mesh.transform.TransformVector(Vector3.forward);
                float depth = Mathf.Abs(Vector3.Dot(outward, localX)) * bounds.extents.x +
                    Mathf.Abs(Vector3.Dot(outward, localY)) * bounds.extents.y +
                    Mathf.Abs(Vector3.Dot(outward, localZ)) * bounds.extents.z;
                MakeText(mesh.transform, key, Center(mesh) + outward * (depth + spacing * 0.015f),
                    rotation, spacing * 0.075f);
            }
        }

        var displayPosition = rows[0].Select(Center).Aggregate(Vector3.zero, (sum, point) => sum + point) / 3;
        CreateDisplay(keypad, displayPosition + up * spacing * 0.85f +
            outward * spacing * 0.55f, rotation, spacing);
        Undo.CollapseUndoOperations(group);
        Debug.Log("World keypad configured. Default code: 1234. Connect On Accepted in the Inspector.", root);
    }

    [MenuItem("Tools/Keypad/Connect Selected Keypad to Scene Code Lock")]
    public static void ConnectToCodeLock()
    {
        var root = Selection.activeGameObject;
        var keypad = root ? root.GetComponent<WorldKeypad>() : null;
        if (!keypad || !root.scene.IsValid())
        {
            Debug.LogError("Select the keypad instance in the scene.");
            return;
        }

        var locks = root.scene.GetRootGameObjects()
            .SelectMany(obj => obj.GetComponentsInChildren<CodeLock>())
            .ToArray();
        if (locks.Length != 1)
        {
            Debug.LogError("Assign the intended Puzzle Lock in the keypad Inspector.", keypad);
            return;
        }

        var controller = new SerializedObject(keypad);
        controller.FindProperty("puzzleLock").objectReferenceValue = locks[0];
        controller.ApplyModifiedProperties();
        Debug.Log("Connected the world keypad to " + locks[0].name + ".", keypad);
    }

    [MenuItem("Tools/Keypad/Upgrade Selected Display")]
    public static void UpgradeDisplay()
    {
        var root = Selection.activeGameObject;
        var keypad = root ? root.GetComponent<WorldKeypad>() : null;
        var oldDisplay = root ? root.transform.Find("Code display") : null;
        if (!keypad || root.transform.Find("Code Display Canvas") || !oldDisplay || !oldDisplay.TryGetComponent<TextMesh>(out var text))
        {
            Debug.LogError("Select the keypad with the original Code display.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Upgrade Keypad Display");
        CreateDisplay(keypad, oldDisplay.position, oldDisplay.rotation, text.characterSize / 0.12f);
        Undo.RecordObject(oldDisplay.gameObject, "Hide original keypad display");
        oldDisplay.gameObject.SetActive(false);
        PrefabUtility.RecordPrefabInstancePropertyModifications(oldDisplay.gameObject);
        Undo.CollapseUndoOperations(group);
    }

    static void CreateDisplay(WorldKeypad keypad, Vector3 position, Quaternion rotation, float spacing)
    {
        var canvasObject = new GameObject("Code Display Canvas", typeof(RectTransform), typeof(Canvas));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create keypad canvas");
        var rect = (RectTransform)canvasObject.transform;
        rect.SetPositionAndRotation(position, rotation);
        rect.localScale = Vector3.one * (spacing / 100f);
        rect.SetParent(keypad.transform, true);
        rect.sizeDelta = new Vector2(280f, 64f);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        var textObject = new GameObject("Entered Code", typeof(RectTransform), typeof(Text));
        Undo.RegisterCreatedObjectUndo(textObject, "Create keypad display text");
        var textRect = (RectTransform)textObject.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 46;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 8;
        text.resizeTextMaxSize = 46;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "----";
        text.raycastTarget = false;

        var controller = new SerializedObject(keypad);
        controller.FindProperty("display").objectReferenceValue = text;
        controller.ApplyModifiedProperties();
    }

    static MeshFilter[][] ChunkForKeypad(this IOrderedEnumerable<MeshFilter> ordered)
    {
        var meshes = ordered.ToArray();
        return Enumerable.Range(0, 4).Select(row => meshes.Skip(row * 3).Take(3).ToArray()).ToArray();
    }

    static TextMesh MakeText(Transform parent, string value, Vector3 position, Quaternion rotation, float size)
    {
        var obj = new GameObject("Keypad text " + value, typeof(TextMesh));
        Undo.RegisterCreatedObjectUndo(obj, "Create keypad text");
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(parent, true);
        var text = obj.GetComponent<TextMesh>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        obj.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        text.text = value;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 64;
        text.characterSize = size;
        text.color = Color.white;
        return text;
    }
}




