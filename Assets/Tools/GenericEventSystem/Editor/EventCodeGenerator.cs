#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace GenericEventSystem.Editor
{
    public static class EventCodeGenerator
    {
        static string pendingEventName;
        static string pendingFolder;
        static double waitUntilTime;

        static void WaitThenCreateAsset()
        {
            if (EditorApplication.timeSinceStartup < waitUntilTime)
                return;

            EditorApplication.update -= WaitThenCreateAsset;

            CreateEventDefinitionAsset();
        }

        public static void GenerateEvent(string eventName, bool shouldCreateEventChannel, string folder, string eventDataCode)
        {
            pendingEventName = eventName;
            pendingFolder = folder;

            if (!Directory.Exists(pendingFolder))
                Directory.CreateDirectory(pendingFolder);

            Write(pendingFolder, $"{eventName}EventData.cs", eventDataCode);

            AssetDatabase.Refresh();
            EditorPrefs.SetBool("CREATE_EVENT_DEF", shouldCreateEventChannel);
            EditorPrefs.SetString("CREATE_EVENT_DEF_FOLDER", pendingFolder);
            EditorPrefs.SetString("CREATE_EVENT_DEF_NAME", pendingEventName);
        }

        [DidReloadScripts]
        static void CreateEventDefinitionAsset()
        {
            if (!EditorPrefs.GetBool("CREATE_EVENT_DEF")) return;

            var assetName = EditorPrefs.GetString("CREATE_EVENT_DEF_NAME");
            var assetPath = $"{EditorPrefs.GetString("CREATE_EVENT_DEF_FOLDER")}/{assetName}.asset";
            var typeName = EditorPrefs.GetString("CREATE_EVENT_DEF_NAME") + "EventData";

            var asset = ScriptableObject.CreateInstance<EventDefinition>();
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.Editor_SetChannelTypeOnce(FindTypeByName(typeName));
            AssetDatabase.SaveAssets();

            EditorPrefs.DeleteKey("CREATE_EVENT_DEF_FOLDER");
            EditorPrefs.DeleteKey("CREATE_EVENT_DEF_NAME");
            EditorPrefs.DeleteKey("CREATE_EVENT_DEF");
        }

        static void Write(string folder, string file, string content)
        {
            File.WriteAllText(Path.Combine(folder, file), content);
        }

        static Type FindTypeByName(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
#endif