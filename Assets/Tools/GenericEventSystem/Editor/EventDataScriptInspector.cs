#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace GenericEventSystem.Editor
{
    [CustomEditor(typeof(MonoScript))]
    public class EventDataScriptInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var script = (MonoScript)target;
            var type = script.GetClass();

            // -------------------------
            // Script + Assembly (readonly)
            // -------------------------
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);

            if (type != null)
                EditorGUILayout.TextField("Assembly", type.Assembly.GetName().Name);

            EditorGUI.EndDisabledGroup();

            if (type == null || !typeof(EventData.EventData).IsAssignableFrom(type))
                return;

            GUILayout.Space(10);

            // -------------------------
            // Fields Section
            // -------------------------
            var fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            // Filter to serialized fields only
            fields = System.Array.FindAll(fields, f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null);

            if (fields.Length > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Fields", EditorStyles.boldLabel);
                GUILayout.Space(2);

                EditorGUI.indentLevel++;

                foreach (var field in fields)
                {
                    // Convert to FieldType and get friendly name
                    var ft = FieldType.FromSystemType(field.FieldType);
                    string typeName = ft.GetDisplayName();
                    string fieldName = ObjectNames.NicifyVariableName(field.Name);

                    EditorGUILayout.LabelField(fieldName, typeName);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // -------------------------
            // Edit Button
            // -------------------------
            if (GUILayout.Button("Edit Event Data", GUILayout.Height(28)))
            {
                string path = AssetDatabase.GetAssetPath(script);
                EventGeneratorWindow.OpenForEdit(path, type);
            }
        }
    }
}
#endif