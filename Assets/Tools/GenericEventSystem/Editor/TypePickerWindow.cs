#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GenericEventSystem.Editor
{
    public class TypePickerWindow : EditorWindow
    {
        private static List<Type> allTypes;
        private static Action<FieldType> onSelect;
        private static FieldType currentType;

        private static Dictionary<Type, string> friendlyNameCache;

        private List<Type> filteredTypes = new List<Type>();

        private string search = "";
        private string lastSearch = null;

        private Vector2 scroll;

        public static void Open(List<Type> runtimeTypes, FieldType current, Action<FieldType> callback)
        {
            allTypes = runtimeTypes;
            currentType = current;
            onSelect = callback;

            friendlyNameCache = new Dictionary<Type, string>(allTypes.Count);
            foreach (var t in allTypes)
            {
                friendlyNameCache[t] = BuildFriendlyName(t);
            }

            var w = CreateInstance<TypePickerWindow>();
            w.titleContent = new GUIContent("Select Type");
            w.minSize = new Vector2(300, 400);
            w.RebuildFilteredList();
            w.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            search = EditorGUILayout.TextField("Search", search);
            if (EditorGUI.EndChangeCheck())
            {
                RebuildFilteredList();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < filteredTypes.Count; i++)
            {
                Type t = filteredTypes[i];
                string name = friendlyNameCache[t];

                if (GUILayout.Button(name))
                {
                    var ft = new FieldType(t);

                    if (t.IsGenericTypeDefinition)
                    {
                        var args = t.GetGenericArguments();
                        for (int a = 0; a < args.Length; a++)
                            ft.GenericArgs.Add(null);
                    }

                    onSelect?.Invoke(ft);
                    Close();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void RebuildFilteredList()
        {
            if (lastSearch == search)
                return;

            lastSearch = search;
            filteredTypes.Clear();

            bool hasSearch = !string.IsNullOrEmpty(search);

            for (int i = 0; i < allTypes.Count; i++)
            {
                var t = allTypes[i];

                if (!hasSearch)
                {
                    filteredTypes.Add(t);
                    continue;
                }

                string friendly = friendlyNameCache[t];

                if (friendly.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (t.FullName != null &&
                     t.FullName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    filteredTypes.Add(t);
                }
            }

            // Fast custom sort
            filteredTypes.Sort((a, b) =>
            {
                string aName = friendlyNameCache[a];
                string bName = friendlyNameCache[b];

                bool aExact = string.Equals(aName, search, StringComparison.OrdinalIgnoreCase);
                bool bExact = string.Equals(bName, search, StringComparison.OrdinalIgnoreCase);

                if (aExact != bExact)
                    return aExact ? -1 : 1;

                bool aStarts = aName.StartsWith(search, StringComparison.OrdinalIgnoreCase);
                bool bStarts = bName.StartsWith(search, StringComparison.OrdinalIgnoreCase);

                if (aStarts != bStarts)
                    return aStarts ? -1 : 1;

                return string.Compare(aName, bName, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static string BuildFriendlyName(Type t)
        {
            if (t.IsGenericTypeDefinition)
            {
                string name = t.Name.Split('`')[0];
                var args = t.GetGenericArguments();

                if (args.Length == 0)
                    return name;

                return $"{name}<{string.Join(", ", args.Select(a => a.Name))}>";
            }

            if (t == typeof(int)) return "int";
            if (t == typeof(string)) return "string";
            if (t == typeof(float)) return "float";
            if (t == typeof(double)) return "double";
            if (t == typeof(bool)) return "bool";

            return t.Name;
        }
    }
}
#endif