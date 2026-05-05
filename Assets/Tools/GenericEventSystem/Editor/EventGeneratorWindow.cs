#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GenericEventSystem.Editor
{
    public class EventGeneratorWindow : EditorWindow
    {
        string eventDataName = "MyEventData";
        bool shouldCreateEventChannel = false;
        string savePath;

        bool isEditMode = false;
        string editingScriptPath;

        List<Type> runtimeTypes;
        List<FieldDefinition> fields = new List<FieldDefinition>();
        Vector2 scroll;

        public static void Open(string defaultPath)
        {
            var w = CreateInstance<EventGeneratorWindow>();
            w.titleContent = new GUIContent("Event Data Generator");
            w.savePath = defaultPath;
            w.minSize = new Vector2(450, 400);
            w.ShowUtility();
        }

        private void OnEnable()
        {
            runtimeTypes = TypeProvider.GetAllRuntimeSafeTypes();
        }

        private void OnGUI()
        {
            GUILayout.Label(
                isEditMode ? "Edit Event Data" : "Event Data Settings",
                EditorStyles.boldLabel
            );

            eventDataName = EditorGUILayout.TextField("Event Data Name", eventDataName);

            GUILayout.Space(8);
            DrawFields();
            GUILayout.Space(6);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(8);

            if (!isEditMode)
            {
                // ---------------- CREATE MODE ONLY ----------------
                DrawSavePath();
                GUILayout.Space(10);

                shouldCreateEventChannel = EditorGUILayout.ToggleLeft(
                    "Create corresponding Event Channel for this Event Data",
                    shouldCreateEventChannel
                );
            }

            GUILayout.FlexibleSpace();

            if (isEditMode)
            {
                if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
                    SaveEventDataChanges();
            }
            else
            {
                if (GUILayout.Button("Generate Event Data", GUILayout.Height(30)))
                    Generate();
            }
        }

        private void DrawFields()
        {
            GUILayout.Label("Fields", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(250));

            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];

                EditorGUILayout.BeginHorizontal();
                field.Name = EditorGUILayout.TextField(field.Name);

                if (field.Type == null)
                    field.Type = new FieldType(typeof(int));

                // Main type picker
                if (GUILayout.Button(field.Type.GetDisplayName()))
                {
                    int fieldIndex = i;
                    TypePickerWindow.Open(runtimeTypes, field.Type, selected =>
                    {
                        field.Type.BaseType = selected.BaseType;
                        field.Type.GenericArgs = selected.GenericArgs;
                    });

                }

                EditorGUI.BeginChangeCheck();
                bool isArray = EditorGUILayout.ToggleLeft("[]", field.Type.IsArray, GUILayout.Width(28));
                if (EditorGUI.EndChangeCheck())
                {
                    field.Type.IsArray = isArray;
                    GUI.changed = true;
                    Repaint();
                }

                // Remove field button
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    fields.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();

                // Draw nested generic args recursively
                DrawGenericArgs(field.Type, 1);
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("+ Add Field"))
                fields.Add(new FieldDefinition { Name = "newField", Type = new FieldType(typeof(int)) });
        }

        private void DrawSavePath()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Save Path", savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder", savePath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    savePath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Initialize GenericArgs placeholders once when assigning a new generic type
        /// </summary>
        private void InitializeGenericArgs(FieldType ft)
        {
            if (ft.BaseType.IsGenericTypeDefinition)
            {
                if (ft.GenericArgs == null)
                    ft.GenericArgs = new List<FieldType>();

                int required = ft.BaseType.GetGenericArguments().Length;
                while (ft.GenericArgs.Count < required)
                    ft.GenericArgs.Add(null);
            }
        }

        /// <summary>
        /// Safely draw generic argument buttons recursively
        /// </summary>
        private void DrawGenericArgs(FieldType ft, int level)
        {
            if (ft == null || !ft.BaseType.IsGenericTypeDefinition || ft.GenericArgs == null)
                return;

            for (int j = 0; j < ft.GenericArgs.Count; j++)
            {
                FieldType arg = ft.GenericArgs[j];

                GUILayout.BeginHorizontal();
                GUILayout.Space(level * 15);

                string displayName = arg != null ? arg.GetDisplayName() : "T";

                if (GUILayout.Button(displayName))
                {
                    int argIndex = j;
                    TypePickerWindow.Open(runtimeTypes, arg ?? new FieldType(typeof(int)), (selected) =>
                    {
                        ft.GenericArgs[argIndex] = selected;
                        InitializeGenericArgs(selected);
                    });
                }

                GUILayout.EndHorizontal();

                // Recurse only if arg is already assigned
                if (arg != null)
                    DrawGenericArgs(arg, level + 1);
            }
        }

        private bool HasDuplicateFieldNames(out string duplicateNames)
        {
            var duplicates = fields
                .GroupBy(f => f.Name)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            duplicateNames = duplicates.Count > 0 ? string.Join(", ", duplicates) : null;
            return duplicates.Count > 0;
        }

        private void Generate()
        {
            if (string.IsNullOrWhiteSpace(eventDataName))
            {
                EditorUtility.DisplayDialog("Error", "Event Data name is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(savePath))
            {
                EditorUtility.DisplayDialog("Error", "Save path is required.", "OK");
                return;
            }

            if (HasDuplicateFieldNames(out string dupNames))
            {
                EditorUtility.DisplayDialog(
                    "Duplicate Fields",
                    $"Duplicate field names: {dupNames}",
                    "OK"
                );
                return;
            }

            string eventFolder = $"{savePath}";

            if (!AssetDatabase.IsValidFolder(eventFolder))
            {
                AssetDatabase.CreateFolder(savePath, eventDataName);
            }

            EventCodeGenerator.GenerateEvent(
                eventDataName,
                shouldCreateEventChannel,
                eventFolder,
                BuildEventDataCode()
            );

            Close();
        }

        private string BuildEventDataCode()
        {
            StringBuilder sb = new StringBuilder();

            var namespaces = fields
                .SelectMany(f => GetNamespaces(f.Type))
                .Distinct()
                .Where(n => !string.IsNullOrEmpty(n))
                .OrderBy(n => n);

            foreach (var ns in namespaces)
                sb.AppendLine($"using {ns};");

            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace GenericEventSystem.EventData");
            sb.AppendLine("{");
            sb.AppendLine("\t[Serializable]");
            sb.AppendLine($"\tpublic class {eventDataName}EventData : EventData");
            sb.AppendLine("\t{");

            foreach (var field in fields)
            {
                sb.AppendLine(
                    $"\t\tpublic {field.Type.GetCodeName(new HashSet<string>(namespaces))} {field.Name};"
                );
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private IEnumerable<string> GetNamespaces(FieldType ft)
        {
            if (ft.BaseType.Namespace != null &&
                ft.BaseType != typeof(string) &&
                ft.BaseType != typeof(int) &&
                ft.BaseType != typeof(float) &&
                ft.BaseType != typeof(double) &&
                ft.BaseType != typeof(bool))
            {
                yield return ft.BaseType.Namespace;
            }

            if (ft.GenericArgs != null)
            {
                foreach (var arg in ft.GenericArgs)
                {
                    if (arg != null)
                    {
                        foreach (var ns in GetNamespaces(arg))
                            yield return ns;
                    }
                }
            }
        }

        private void LoadFromExistingScript(string scriptPath, Type eventType)
        {
            eventDataName = eventType.Name.Replace("EventData", "");
            savePath = Path.GetDirectoryName(scriptPath);

            fields = new List<FieldDefinition>();

            var existingFields = eventType.GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly
            );

            foreach (var f in existingFields)
            {
                fields.Add(new FieldDefinition
                {
                    Name = f.Name,
                    Type = FieldType.FromSystemType(f.FieldType)
                });
            }

            shouldCreateEventChannel = File.Exists(
                Path.Combine(savePath, eventDataName + "Event.asset")
            );
        }

        public static void OpenForEdit(string scriptPath, Type eventType)
        {
            var w = CreateInstance<EventGeneratorWindow>();
            w.titleContent = new GUIContent("Edit Event Data");
            w.minSize = new Vector2(450, 400);

            w.isEditMode = true;
            w.editingScriptPath = scriptPath;

            w.LoadFromExistingScript(scriptPath, eventType);

            w.ShowUtility();
        }

        private void SaveEventDataChanges()
        {
            if (string.IsNullOrEmpty(editingScriptPath))
                return;

            if (HasDuplicateFieldNames(out string dupNames))
            {
                EditorUtility.DisplayDialog(
                    "Duplicate Fields",
                    $"Duplicate field names: {dupNames}",
                    "OK"
                );
                return;
            }

            File.WriteAllText(editingScriptPath, BuildEventDataCode());

            AssetDatabase.Refresh();
            Close();
        }
    }
}
#endif