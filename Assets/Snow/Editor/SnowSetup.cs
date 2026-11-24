using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu: Tools > Snow
/// Zet de sneeuw in de AlexSnow-scene (Game/Section 1/Room 2, de lockerkamer),
/// of op een vloer die je zelf selecteert.
/// </summary>
public static class SnowSetup
{
    const string ScenePath = "Assets/Scenes/AlexSnow.unity";
    const string RoomPath = "Game/Section 1/Room 2";
    const string MaterialPath = "Assets/Snow/SnowMaterial.mat";

    [MenuItem("Tools/Snow/Setup AlexSnow (Room 2 - lockerkamer)")]
    static void SetupAlexSnow()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Transform room = FindInScene(scene, RoomPath);
        Transform floor = room != null ? room.Find("Floor") : null;
        if (floor == null)
        {
            EditorUtility.DisplayDialog("Snow", $"Kon '{RoomPath}/Floor' niet vinden in de scene.\n" +
                "Selecteer de vloer zelf en gebruik Tools > Snow > Add Snow To Selected Floor.", "OK");
            return;
        }

        GameObject snow = CreateSnowOn(floor.gameObject);
        if (snow == null) return;
        int count = AddFootprintsToCharacters(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = snow;
        EditorGUIUtility.PingObject(snow);
        Debug.Log($"[Snow] Sneeuw geplaatst in {RoomPath}, footprints op {count} character(s). Scene opgeslagen.");
    }

    [MenuItem("Tools/Snow/Add Snow To Selected Floor")]
    static void AddToSelected()
    {
        GameObject floor = Selection.activeGameObject;
        GameObject snow = CreateSnowOn(floor);
        if (snow == null) return;
        int count = AddFootprintsToCharacters(floor.scene);
        EditorSceneManager.MarkSceneDirty(floor.scene);
        Selection.activeGameObject = snow;
        Debug.Log($"[Snow] Sneeuw geplaatst op '{floor.name}', footprints op {count} character(s). Vergeet niet op te slaan (Ctrl+S).");
    }

    [MenuItem("Tools/Snow/Add Snow To Selected Floor", true)]
    static bool AddToSelectedValidate() =>
        Selection.activeGameObject != null && Selection.activeGameObject.scene.IsValid();

    // ------------------------------------------------------------------

    static Transform FindInScene(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        string rootName = slash < 0 ? path : path.Substring(0, slash);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != rootName) continue;
            if (slash < 0) return root.transform;
            Transform t = root.transform.Find(path.Substring(slash + 1));
            if (t != null) return t;
        }
        return null;
    }

    static GameObject CreateSnowOn(GameObject floor)
    {
        if (!TryGetBounds(floor, out Bounds b))
        {
            EditorUtility.DisplayDialog("Snow", $"'{floor.name}' heeft geen Renderer of Collider, dus ik weet niet hoe groot de vloer is.", "OK");
            return null;
        }

        Material mat = GetOrCreateMaterial();
        Shader updateShader = Shader.Find("Hidden/SnowTrailUpdate");
        if (mat == null || updateShader == null)
        {
            EditorUtility.DisplayDialog("Snow", "De snow-shaders zijn (nog) niet gevonden. Wacht tot Unity klaar is met importeren en probeer opnieuw.", "OK");
            return null;
        }

        // Het systeem ondersteunt één sneeuwveld tegelijk: oude weghalen
        foreach (SnowSurface old in Object.FindObjectsByType<SnowSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old.gameObject.scene == floor.scene)
                Undo.DestroyObjectImmediate(old.gameObject);

        var go = new GameObject("Snow");
        go.SetActive(false); // eerst alles instellen, dan pas aanzetten
        Undo.RegisterCreatedObjectUndo(go, "Create Snow");
        SceneManager.MoveGameObjectToScene(go, floor.scene);

        // Netjes onder de kamer hangen als die niet gedraaid/geschaald is
        Transform parent = floor.transform.parent;
        if (parent != null && parent.rotation == Quaternion.identity && parent.lossyScale == Vector3.one)
            go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(new Vector3(b.center.x, b.max.y, b.center.z), Quaternion.identity);
        go.transform.localScale = Vector3.one;

        var snow = go.AddComponent<SnowSurface>();
        snow.size = new Vector2(b.size.x, b.size.z);
        snow.snowThickness = 0.15f;
        float longest = Mathf.Max(b.size.x, b.size.z);
        snow.meshResolution = Mathf.Clamp(Mathf.CeilToInt(longest / 0.035f), 64, 512); // ~3,5 cm per vakje
        snow.trailTextureSize = longest > 20f ? 2048 : 1024;
        snow.fadeDuration = 30f;
        snow.conformToGround = false; // binnen: de raycast zou anders het plafond raken
        snow.trailUpdateShader = updateShader;

        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        go.SetActive(true);
        EditorUtility.SetDirty(snow);
        return go;
    }

    static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!found) { bounds = r.bounds; found = true; } else bounds.Encapsulate(r.bounds);
        }
        if (found) return true;
        foreach (Collider c in go.GetComponentsInChildren<Collider>())
        {
            if (!found) { bounds = c.bounds; found = true; } else bounds.Encapsulate(c.bounds);
        }
        return found;
    }

    static Material GetOrCreateMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;
        Shader shader = Shader.Find("Custom/DeformableSnow");
        if (shader == null) return null;
        mat = new Material(shader) { name = "SnowMaterial" };
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    /// <summary>Player = CharacterController, enemies = NavMeshAgent.</summary>
    static int AddFootprintsToCharacters(Scene scene)
    {
        // Grond = alles behalve de player/enemy/ignore-raycast layers
        int groundMask = ~LayerMask.GetMask("Player", "Enemy", "Ignore Raycast", "UI");

        var targets = new List<(GameObject go, bool isEnemy)>();
        foreach (var cc in Object.FindObjectsByType<UnityEngine.CharacterController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (cc.gameObject.scene == scene) targets.Add((cc.gameObject, false));
        foreach (var agent in Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (agent.gameObject.scene == scene) targets.Add((agent.gameObject, true));

        foreach (var (go, isEnemy) in targets)
        {
            var fp = go.GetComponent<SnowFootprints>();
            if (fp == null) fp = Undo.AddComponent<SnowFootprints>(go); // scene-override, prefab blijft ongewijzigd
            else Undo.RecordObject(fp, "Configure Footprints");

            fp.groundLayers = groundMask;
            fp.requireGrounded = true;
            if (isEnemy)
            {
                fp.stepDistance = 0.75f;
                fp.footSpacing = 0.18f;
                fp.footprintWidth = 0.16f;
                fp.footprintLength = 0.36f;
            }
            else
            {
                fp.stepDistance = 0.65f;
                fp.footSpacing = 0.14f;
                fp.footprintWidth = 0.13f;
                fp.footprintLength = 0.30f;
            }
            EditorUtility.SetDirty(fp);
            PrefabUtility.RecordPrefabInstancePropertyModifications(fp);
        }
        return targets.Count;
    }
}
