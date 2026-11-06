using UnityEditor;
using UnityEngine;

namespace CharacterController.EditorScripts
{
    [CustomEditor(typeof(CharacterController.InputManager))]
    public class InputManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();

#if ENABLE_INPUT_SYSTEM
            EditorGUILayout.HelpBox("Input System: New Input System Package", MessageType.Info);
#else
            EditorGUILayout.HelpBox("Input System: Legacy Input Manager", MessageType.Info);
#endif

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
