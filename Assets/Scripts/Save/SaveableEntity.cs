using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
    public const string SceneToken = "{scene}";

    [SerializeField] string id;
    [Tooltip("Save file of this object. " + SceneToken + " is replaced by the scene name (e.g. \"player\" for global data).")]
    [SerializeField] string file = "level_" + SceneToken;
    [Tooltip("Save and restore activeSelf (picked up items, opened areas...).")]
    [SerializeField] bool saveActiveState = true;

    public string Id => id;

    public string GetFile(Scene scene) => file.Replace(SceneToken, scene.name);

    public EntityRecord Capture()
    {
        var record = new EntityRecord();
        if (saveActiveState) record.active = gameObject.activeSelf;

        foreach (ISaveable saveable in GetComponents<ISaveable>())
        {
            try
            {
                record.components[saveable.SaveKey] = new ComponentRecord
                {
                    version = saveable.StateVersion,
                    data = saveable.CaptureJson()
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] {name}/{saveable.SaveKey}: capture failed ({e.Message})", this);
            }
        }
        return record;
    }

    public void Restore(EntityRecord record)
    {
        if (saveActiveState && record.active.HasValue) gameObject.SetActive(record.active.Value);

        foreach (ISaveable saveable in GetComponents<ISaveable>())
        {
            if (!record.components.TryGetValue(saveable.SaveKey, out ComponentRecord component) || component.data == null)
                continue;

            try
            {
                var data = component.data;
                for (int version = component.version; version < saveable.StateVersion; version++)
                    data = saveable.Migrate(data, version);
                saveable.RestoreJson(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] {name}/{saveable.SaveKey}: saved state ignored, keeping defaults ({e.Message})", this);
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;

        if (PrefabUtility.IsPartOfPrefabAsset(this) || PrefabStageUtility.GetPrefabStage(gameObject) != null)
        {
            if (!string.IsNullOrEmpty(id)) SetId("");
            return;
        }

        if (string.IsNullOrEmpty(id) || IsDuplicate()) SetId(Guid.NewGuid().ToString("N"));
    }

    [ContextMenu("Regenerate Id")]
    void RegenerateId() => SetId(Guid.NewGuid().ToString("N"));

    void SetId(string value)
    {
        id = value;
        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    bool IsDuplicate()
    {
        foreach (SaveableEntity other in FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (other != this && other.id == id) return true;
        return false;
    }
#endif
}
