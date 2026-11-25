using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    public static bool AutoRestore = true;

    public static string SaveDirectory => Path.Combine(Application.persistentDataPath, "saves");

    public const string SettingsFile = "settings";

    public static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        Converters = { new StringEnumConverter(), new UnityTypesConverter() },
        Error = (_, args) =>
        {
            if (args.CurrentObject == args.ErrorContext.OriginalObject)
                Debug.LogWarning($"[Save] '{args.ErrorContext.Path}' ignored: {args.ErrorContext.Error.Message}");
            args.ErrorContext.Handled = true;
        }
    };

    public static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

    static readonly Dictionary<string, SaveFile> files = new();
    static readonly HashSet<string> dirty = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        AutoRestore = true;
        files.Clear();
        dirty.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (AutoRestore) RestoreScene(scene);
    }

    static public void ResetLevel()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }


    // ---- Manage Save ----

    public static bool HasSave() =>
        Directory.Exists(SaveDirectory)
        && Directory.GetFiles(SaveDirectory, "*.json").Any(path => !IsSettingsFile(path));

    public static void DeleteSave()
    {
        if (Directory.Exists(SaveDirectory))
        {
            foreach (string path in Directory.GetFiles(SaveDirectory))
                if (!IsSettingsFile(path)) File.Delete(path);
            foreach (string dir in Directory.GetDirectories(SaveDirectory))
                Directory.Delete(dir, true);
        }
        ReloadFromDisk();
    }

    public static void ReloadFromDisk()
    {
        Flush(SettingsFile);
        files.Clear();
        dirty.Clear();
    }

    static bool IsSettingsFile(string path) => Path.GetFileName(path).StartsWith(SettingsFile + ".json");


    // ---- Scenes ----

    // Captures every loaded scene then writes to disk.
    public static void Save()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded) CaptureScene(scene);
        }
        Flush();
    }

    // Call before unloading a scene to keep its state in memory until the next Save.
    public static void CaptureScene(Scene scene)
    {
        foreach (SaveableEntity entity in FindEntities(scene))
        {
            string fileName = entity.GetFile(scene);
            GetFile(fileName).entities[entity.Id] = entity.Capture();
            dirty.Add(fileName);
        }
    }

    public static void RestoreScene(Scene scene)
    {
        foreach (SaveableEntity entity in FindEntities(scene))
        {
            if (GetFile(entity.GetFile(scene)).entities.TryGetValue(entity.Id, out EntityRecord record))
                entity.Restore(record);
        }
    }

    public static SaveableEntity FindEntity(string id)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (SaveableEntity entity in FindEntities(scene))
                if (entity.Id == id) return entity;
        }
        return null;
    }

    static IEnumerable<SaveableEntity> FindEntities(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SaveableEntity entity in root.GetComponentsInChildren<SaveableEntity>(true))
            {
                if (string.IsNullOrEmpty(entity.Id))
                    Debug.LogWarning($"[Save] {entity.name} has no id and is skipped (runtime-spawned?)", entity);
                else
                    yield return entity;
            }
        }
    }


    // ---- Free values ----

    public static void Set<T>(string file, string key, T value)
    {
        GetFile(file).values[key] = value == null ? JValue.CreateNull() : JToken.FromObject(value, Serializer);
        dirty.Add(file);
    }

    public static T Get<T>(string file, string key, T defaultValue = default)
    {
        if (!GetFile(file).values.TryGetValue(key, out JToken token)) return defaultValue;
        try
        {
            return token.ToObject<T>(Serializer);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] {file}/{key}: {e.Message}, using default");
            return defaultValue;
        }
    }

    public static bool HasKey(string file, string key) => GetFile(file).values.ContainsKey(key);

    public static void DeleteKey(string file, string key)
    {
        if (GetFile(file).values.Remove(key)) dirty.Add(file);
    }


    // ---- Disk ----

    public static void Flush()
    {
        foreach (string name in dirty) Write(name, files[name]);
        dirty.Clear();
    }

    public static void Flush(string file)
    {
        if (dirty.Remove(file)) Write(Sanitize(file), files[Sanitize(file)]);
    }

    static SaveFile GetFile(string name)
    {
        name = Sanitize(name);
        if (files.TryGetValue(name, out SaveFile file)) return file;

        string path = PathOf(name);
        file = Read(name, path) ?? Read(name, path + ".bak") ?? new SaveFile();
        files[name] = file;
        return file;
    }

    static SaveFile Read(string name, string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            JObject root = JObject.Parse(File.ReadAllText(path));
            int format = root.Value<int?>("format") ?? 1;
            if (format > SaveFile.CurrentFormat)
                Debug.LogWarning($"[Save] {path} comes from a newer game version (format {format}), loading what can be read");

            for (; format < SaveFile.CurrentFormat; format++) MigrateFormat(root, format);

            int valuesVersion = root.Value<int?>("valuesVersion") ?? 1;
            if (root["values"] is JObject values)
            {
                for (; valuesVersion < ValuesMigrations.CurrentVersion; valuesVersion++)
                    ValuesMigrations.Migrate(name, values, valuesVersion);
            }

            SaveFile file = root.ToObject<SaveFile>(Serializer) ?? new SaveFile();
            file.format = SaveFile.CurrentFormat;
            file.valuesVersion = ValuesMigrations.CurrentVersion;
            return file;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] cannot read {path}: {e.Message}");
            return null;
        }
    }

    // Upgrades the SaveFile layout itself.
    static void MigrateFormat(JObject root, int format)
    {
        // if (format == 1)
        // {
        //     if (root["values"] is JObject values && values["scene"] != null)
        //     {
        //         values["currentLevel"] = values["scene"];
        //         values.Remove("scene");
        //     }
        // }
    }

    static void Write(string name, SaveFile file)
    {
        string path = PathOf(name);
        string temp = path + ".tmp";
        try
        {
            file.gameVersion = Application.version;
            file.savedAt = DateTime.UtcNow.ToString("o");
            Directory.CreateDirectory(SaveDirectory);

            File.WriteAllText(temp, JsonConvert.SerializeObject(file, Settings));
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] cannot write {path}: {e.Message}");
        }
    }

    static string PathOf(string name) => Path.Combine(SaveDirectory, name + ".json");

    static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}
