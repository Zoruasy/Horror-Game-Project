using System.IO;
using UnityEditor;

static class SaveSystemMenu
{
    [MenuItem("Tools/Save/Open Save Folder")]
    static void OpenFolder()
    {
        Directory.CreateDirectory(SaveSystem.SaveDirectory);
        EditorUtility.RevealInFinder(SaveSystem.SaveDirectory);
    }

    [MenuItem("Tools/Save/Delete Save")]
    static void DeleteSave()
    {
        if (!EditorUtility.DisplayDialog("Delete save", $"Delete everything in\n{SaveSystem.SaveDirectory} ?", "Delete", "Cancel")) return;
        SaveSystem.DeleteSave();
    }
}
