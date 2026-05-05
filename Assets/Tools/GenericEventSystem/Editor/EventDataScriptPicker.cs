#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GenericEventSystem.Editor
{
    public class EventDataScriptPicker : EditorWindow
    {
        private MonoScript[] scripts;
        private Texture2D icon;
        private Vector2 scroll;
        private string search = "";
        private Action<MonoScript> onPick;

        public static void Open(Action<MonoScript> onPickCallback, Texture2D eventDataIcon)
        {
            var w = CreateInstance<EventDataScriptPicker>();
            w.titleContent = new GUIContent("Select EventData Script");
            w.minSize = new Vector2(300, 400);
            w.onPick = onPickCallback;
            w.icon = eventDataIcon;
            w.FetchScripts();
            w.ShowUtility();
        }

        private void FetchScripts()
        {
            var guids = AssetDatabase.FindAssets("t:MonoScript");
            scripts = guids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Select(p => AssetDatabase.LoadAssetAtPath<MonoScript>(p))
                .Where(s => s != null && s.GetClass() != null && typeof(EventData.EventData).IsAssignableFrom(s.GetClass()))
                .OrderBy(s => s.name)
                .ToArray();
        }

        private void OnGUI()
        {
            GUILayout.Label("Search:", EditorStyles.boldLabel);
            search = EditorGUILayout.TextField(search);

            GUILayout.Space(4);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width - 16));

            foreach (var script in scripts)
            {
                if (!string.IsNullOrEmpty(search) &&
                    !script.name.ToLower().Contains(search.ToLower()))
                    continue;

                EditorGUIUtility.SetIconSize(new Vector2(16, 16));

                var content = new GUIContent(script.name, icon);

                if (GUILayout.Button(content, EditorStyles.objectField))
                {
                    onPick?.Invoke(script);
                    Close();
                }

                GUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            if (GUILayout.Button("Cancel"))
                Close();
        }
    }
}
#endif