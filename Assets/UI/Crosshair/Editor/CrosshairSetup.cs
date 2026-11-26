using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools > UI
/// Zet CrosshairUI op de crosshair ("pointer") in de geopende scene en koppelt de sprites.
/// </summary>
public static class CrosshairSetup
{
    const string DotPath = "Assets/UI/Crosshair/Crosshair_Dot.png";
    const string HandPath = "Assets/UI/Crosshair/Crosshair_Hand.png";
    const string PointerName = "pointer";

    [MenuItem("Tools/UI/Setup Crosshair Hand (huidige scene)")]
    static void SetupCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        Image pointer = FindPointer(scene);
        if (pointer == null)
        {
            EditorUtility.DisplayDialog("Crosshair",
                $"Geen Image met de naam '{PointerName}' gevonden in '{scene.name}'.\n" +
                "Selecteer je crosshair-Image en gebruik Tools > UI > Setup Crosshair Hand (geselecteerde Image).", "OK");
            return;
        }
        if (Setup(pointer))
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Crosshair] Hand-crosshair ingesteld op '{GetPath(pointer.transform)}' in {scene.name}. Scene opgeslagen.");
        }
    }

    [MenuItem("Tools/UI/Setup Crosshair Hand (geselecteerde Image)")]
    static void SetupSelected()
    {
        var img = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<Image>() : null;
        if (img == null) { EditorUtility.DisplayDialog("Crosshair", "Selecteer een GameObject met een Image-component.", "OK"); return; }
        if (Setup(img)) Debug.Log("[Crosshair] Ingesteld. Vergeet niet op te slaan (Ctrl+S).");
    }

    static bool Setup(Image pointer)
    {
        EnsureSpriteImport(DotPath);
        EnsureSpriteImport(HandPath);
        Sprite dot = AssetDatabase.LoadAssetAtPath<Sprite>(DotPath);
        Sprite hand = AssetDatabase.LoadAssetAtPath<Sprite>(HandPath);
        if (dot == null || hand == null)
        {
            EditorUtility.DisplayDialog("Crosshair", $"Kon de sprites niet laden:\n{DotPath}\n{HandPath}", "OK");
            return false;
        }

        PlayerInteractor interactor = null;
        foreach (var pi in Object.FindObjectsByType<PlayerInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (pi.gameObject.scene == pointer.gameObject.scene) { interactor = pi; break; }
        if (interactor == null)
            Debug.LogWarning("[Crosshair] Geen PlayerInteractor in de scene gevonden; hij zoekt er in Play mode zelf naar.");

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

    // Zorgt dat de png's als UI-sprite geïmporteerd zijn
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
