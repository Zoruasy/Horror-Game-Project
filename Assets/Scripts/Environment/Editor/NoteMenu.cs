using UnityEditor;
using UnityEngine;

static class NoteMenu
{
    [MenuItem("GameObject/3D Object/Note", false, 10)]
    [MenuItem("Tools/Notes/Create Note")]
    static void CreateNote(MenuCommand command)
    {
        GameObject note = GameObject.CreatePrimitive(PrimitiveType.Quad);
        note.name = "Note";
        Object.DestroyImmediate(note.GetComponent<MeshCollider>());
        note.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.01f);

        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Quad);
        back.name = "Back";
        Object.DestroyImmediate(back.GetComponent<MeshCollider>());
        back.transform.SetParent(note.transform, false);
        back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var component = note.AddComponent<Note>();
        var so = new SerializedObject(component);
        so.FindProperty("back").objectReferenceValue = back.GetComponent<Renderer>();
        so.ApplyModifiedPropertiesWithoutUndo();

        if (command.context is GameObject parent) note.transform.SetParent(parent.transform, false);
        else if (SceneView.lastActiveSceneView) note.transform.position = SceneView.lastActiveSceneView.pivot;

        Undo.RegisterCreatedObjectUndo(note, "Create Note");
        Selection.activeGameObject = note;
    }
}
