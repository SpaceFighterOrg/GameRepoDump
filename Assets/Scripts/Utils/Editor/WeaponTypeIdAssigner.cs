#if UNITY_EDITOR
using Assets.Scripts.MVVM.Models.WeaponTypes;
using UnityEditor;
using UnityEngine;

public class WeaponTypeIdAssigner : AssetPostprocessor
{
    private static bool _isAssigning = false;

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (_isAssigning) return;

        foreach (string assetPath in importedAssets)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(WeaponType))
            {
                AssignAllIds();
                return;
            }
        }
    }

    [MenuItem("Tools/Reassign Weapon Type IDs")]
    public static void AssignAllIds()
    {
        _isAssigning = true;

        try
        {
            var guids = AssetDatabase.FindAssets("t:WeaponType");
            int id = 0;
            bool anyDirty = false;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponType>(path);
                if (weapon == null) continue;

                if (weapon.Id != id)
                {
                    weapon.Id = id;
                    EditorUtility.SetDirty(weapon);
                    anyDirty = true;
                }
                id++;
            }

            if (anyDirty)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[WeaponTypeIdAssigner] IDs reassigned successfully.");
            }
        }
        finally
        {
            _isAssigning = false;
        }
    }
}
#endif