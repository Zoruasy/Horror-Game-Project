using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;

public class PrefabColorResetter : Editor
{
    [MenuItem("Tools/ProBuilder/Sync Colors and Relink to Prefab")]
    public static void SyncAndRelink()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int count = 0;

        foreach (GameObject obj in selectedObjects)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                ProBuilderMesh pbMesh = obj.GetComponent<ProBuilderMesh>();
                if (pbMesh != null)
                {
                    // 1. We registreren een Undo-stap voor de veiligheid
                    Undo.RecordObject(pbMesh, "Relink ProBuilder to Prefab");

                    // 2. Haal de originele prefab op
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabSource != null)
                    {
                        ProBuilderMesh sourceMesh = prefabSource.GetComponent<ProBuilderMesh>();

                        // 3. Haal de kleur van de prefab op
                        Color prefabColor = Color.red;
                        if (sourceMesh != null && sourceMesh.colors != null && sourceMesh.colors.Count > 0)
                        {
                            prefabColor = sourceMesh.colors[0];
                        }

                        // 4. Maak de kleurenlijst passend voor de unieke vorm in Level 2
                        System.Collections.Generic.List<Color> newColors = new System.Collections.Generic.List<Color>();
                        for (int i = 0; i < pbMesh.vertexCount; i++)
                        {
                            newColors.Add(prefabColor);
                        }

                        // 5. Pas de kleuren toe en ververs de mesh data
                        pbMesh.colors = newColors;
                        pbMesh.ToMesh();
                        pbMesh.Refresh();

                        // ===== DE FIX =====
                        // 6. We vertellen Unity dat de vertex-kleuren en mesh-aanpassingen vanaf nu GEEN override meer zijn.
                        // Hierdoor wordt de "blauwe lijn" in de Inspector bij ProBuilderMesh weggehaald en luistert hij weer naar de Asset!
                        PrefabUtility.RevertObjectOverride(pbMesh, InteractionMode.AutomatedAction);

                        count++;
                    }
                }
            }
        }
        Debug.Log($"Succesvol {count} muren gekoppeld! Pas nu de prefab aan en Level 2 kleurt direct mee.");
    }
}
