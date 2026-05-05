#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace GenericEventSystem.Editor
{
    public static class EventGeneratorMenu
    {
        [MenuItem("Assets/Create/Generic Event System/Event Data", false, 80)]
        static void Open()
        {
            var path = "Assets";
            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (File.Exists(path))
                    path = Path.GetDirectoryName(path);
                break;
            }

            EventGeneratorWindow.Open(path);
        }
    }
}
#endif