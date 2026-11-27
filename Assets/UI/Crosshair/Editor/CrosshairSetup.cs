using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools > UI
/// Adds CrosshairUI to the crosshair ("pointer") in the open scene and assigns the sprites.
/// </summary>
public static class CrosshairSetup
{
    const string DotPath = "Assets/UI/Crosshair/Crosshair_Dot.png";
    const string HandPath = "Assets/UI/Crosshair/Crosshair_Hand.png";
    const string PointerName = "pointer";

    [MenuItem("Tools/UI/Setup Crosshair Hand (current scene)")]
    static void SetupCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        Image pointer = FindPointer(scene);
        if (pointer == null)
        {
            EditorUtility.DisplayDialog("Crosshair",
                $"No Image named '{PointerName}' found in '{scene.name}'.\n" +
                "Select your crosshair Image and use Tools > UI > Setup Crosshair Hand (selected Image).", "OK");
            return;
        }
        if (Setup(pointer))
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Crosshair] Hand crosshair set up on '{GetPath(pointer.transform)}' in {scene.name}. Scene saved.");
        }
    }

    [MenuItem("Tools/UI/Setup Crosshair Hand (selected Image)")]
    static void SetupSelected()
    {
        var img = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Image>() : null;
        if (img == null) { EditorUtility.DisplayDialog("Crosshair", "Select a GameObject with an Image component.", "OK"); return; }
        if (Setup(img)) Debug.Log("[Crosshair] Done. Don't forget to save (Ctrl+S).");
    }

    static bool Setup(Image pointer)
    {
        EnsureSpriteImport(DotPath);
        EnsureSpriteImport(HandPath);
        Sprite dot = AssetDatabase.LoadAssetAtPath<Sprite>(DotPath);
        Sprite hand = AssetDatabase.LoadAssetAtPath<Sprite>(HandPath);
        if (dot == null || hand == null)
        {
            EditorUtility.DisplayDialog("Crosshair", $"Could not load the sprites:\n{DotPath}\n{HandPath}", "OK");
            return false;
        }

        PlayerInteractor interactor = null;
        foreach (var pi in Object.FindObjectsByType<PlayerInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (pi.gameObject.scene == pointer.gameObject.scene) { interactor = pi; break; }
        if (interactor == null)
            Debug.LogWarning("[Crosshair] No PlayerInteractor found in the scene; it will search for one itself in Play mode.");

        Undo.RecordObject(pointer, "Setup Crosshair");
        pointer.sprite = dot;
        pointer.raycastTarget = false;
        pointer.preserveAspect = true;

        var rect = (RectTransform)pointer.transform;
        Undo.RecordObject(rect, "Setup Crosshair");
        rect.sizeDelta = new Vector2(10f, 10f);

        var ui = pointer.GetComponent<CrosshairUI>();
        if (ui == null) ui = Undo.AddComponent<CrosshairUI>(pointer.gameObject);

        var so = new SerializedObject(ui);
        so.FindProperty("interactor").objectReferenceValue = interactor;
        so.FindProperty("image").objectReferenceValue = pointer;
        so.FindProperty("defaultSprite").objectReferenceValue = dot;
        so.FindProperty("interactSprite").objectReferenceValue = hand;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(pointer.gameObject.scene);
        Selection.activeGameObject = pointer.gameObject;
        return true;
    }

    static Image FindPointer(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Image img in root.GetComponentsInChildren<Image>(true))
                if (img.name == PointerName) return img;
        return null;
    }

    // Makes sure the PNGs are imported as UI sprites
    static void EnsureSpriteImport(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        bool changed = false;
        if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
        if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
        if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
        if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
        if (changed) ti.SaveAndReimport();
    }

    static string GetPath(Transform t) => t.parent ? GetPath(t.parent) + "/" + t.name : t.name;
}
