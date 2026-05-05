#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GenericEventSystem.Editor
{
    [InitializeOnLoad]
    public static class EventIconReplacer
    {
        static EventIconReplacer()
        {
            EditorApplication.delayCall += ApplyIconsOnce;
        }

        // --------------------------------------------------
        // Apply Icons Safely (Persistent)
        // --------------------------------------------------

        private static void ApplyIconsOnce()
        {
            var eventIcon = LoadIcon("event_icon.png");
            var eventDataIcon = LoadIcon("event_data_icon.png");

            if (eventIcon == null || eventDataIcon == null)
                return;

            bool changed = false;

            var guids = AssetDatabase.FindAssets("t:MonoScript");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (monoScript == null)
                    continue;

                var type = monoScript.GetClass();
                if (type == null)
                    continue;

                var importer = AssetImporter.GetAtPath(path) as MonoImporter;
                if (importer == null)
                    continue;

                Texture2D desiredIcon = null;

                // EventData scripts
                if (typeof(EventData.EventData).IsAssignableFrom(type))
                {
                    desiredIcon = eventDataIcon;
                }
                // EventDefinition script
                else if (type == typeof(EventDefinition))
                {
                    desiredIcon = eventIcon;
                }
                else
                {
                    continue;
                }

                // Compare by asset path (NOT by reference)
                var currentIcon = importer.GetIcon();

                if (currentIcon != null)
                {
                    string currentPath = AssetDatabase.GetAssetPath(currentIcon);
                    if (currentPath == AssetDatabase.GetAssetPath(desiredIcon))
                        continue;
                }

                importer.SetIcon(desiredIcon);
                importer.SaveAndReimport();
                changed = true;
            }

            if (changed)
                EditorApplication.RepaintProjectWindow();
        }

        // --------------------------------------------------
        // Helpers
        // --------------------------------------------------

        private static string GetPluginFolder()
        {
            // Find this script in the project
            var guids = AssetDatabase.FindAssets("EventIconReplacer t:Script");
            if (guids.Length == 0)
                throw new Exception("Cannot find EventIconReplacer script in project!");

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return Path.GetDirectoryName(path).Replace("\\", "/");
        }

        private static Texture2D LoadIcon(string iconFileName)
        {
            string folder = GetPluginFolder(); // <-- safe, guaranteed to exist
            string iconPath = Path.Combine(folder, "Icons", iconFileName).Replace("\\", "/");

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (icon == null)
                Debug.LogError($"Icon not found at {iconPath}");

            return icon;
        }
    }
}
#endif