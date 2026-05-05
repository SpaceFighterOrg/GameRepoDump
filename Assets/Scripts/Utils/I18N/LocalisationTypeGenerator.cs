#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Utils.I18N
{
    public class LocalisationTypeGenerator : AssetPostprocessor
    {
        private const string CsvResourcePath = "Assets/Resources/i18n.csv";
        private const string LanguageOutputPath = "Assets/Scripts/Utils/I18N/Languages.cs";
        private const string KeysOutputPath = "Assets/Scripts/Utils/I18N/LocalisationKeys.cs";
        private const string Namespace = "Assets.Scripts.Utils.I18N";

        [MenuItem("Tools/Localization/Generate Types")]
        public static void GenerateFromMenu()
        {
            Generate();
            AssetDatabase.Refresh();
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (path == CsvResourcePath)
                {
                    Debug.Log("[LocalisationTypeGenerator] CSV changed — regenerating types.");
                    Generate();
                    AssetDatabase.Refresh();
                    return;
                }
            }
        }

        private static void Generate()
        {
            if (!File.Exists(CsvResourcePath))
            {
                Debug.LogError($"[LocalisationTypeGenerator] CSV not found at: {CsvResourcePath}");
                return;
            }

            (List<string> languages, List<string> keys) = ReadCSV(CsvResourcePath);

            if (languages.Count == 0)
            {
                Debug.LogWarning("[LocalisationTypeGenerator] No language names found in CSV — nothing generated.");
                return;
            }

            if (keys.Count == 0)
            {
                Debug.LogWarning("[LocalisationTypeGenerator] No keys found in CSV — nothing generated.");
                return;
            }

            WriteFile(LanguageOutputPath, BuildEnumSource("Languages", languages));
            WriteFile(KeysOutputPath, BuildEnumSource("LocalisationKeys", keys));

            Debug.Log($"[LocalisationTypeGenerator] Generated {languages.Count} languages → {LanguageOutputPath}");
            Debug.Log($"[LocalisationTypeGenerator] Generated {keys.Count} keys → {KeysOutputPath}");
        }

        private static (List<string> languages, List<string> keys) ReadCSV(string path)
        {
            var languages = new List<string>();
            var keys = new List<string>();

            using var reader = new StreamReader(path, Encoding.UTF8);

            // keys
            string headerLine = reader.ReadLine();
            if (headerLine != null)
            {
                List<string> headerCells = SplitCSVLine(headerLine);
                for (int col = 1; col < headerCells.Count; col++)
                {
                    string key = headerCells[col].Trim();
                    if (!string.IsNullOrEmpty(key))
                        keys.Add(key);
                }
            }

            // languages
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                List<string> cells = SplitCSVLine(line);
                if (cells.Count == 0) continue;

                string lang = cells[0].Trim();
                if (!string.IsNullOrEmpty(lang))
                    languages.Add(lang);
            }

            return (languages, keys);
        }

        private static List<string> SplitCSVLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else if (c == '"') inQuotes = false;
                    else current.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                    else current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields;
        }

        private static string BuildEnumSource(string enumName, List<string> values)
        {
            var sb = new StringBuilder();

            bool hasNamespace = !string.IsNullOrWhiteSpace(Namespace);
            if (hasNamespace)
            {
                sb.AppendLine($"namespace {Namespace}");
                sb.AppendLine("{");
            }

            string indent = hasNamespace ? "    " : "";

            sb.AppendLine($"{indent}public enum {enumName}");
            sb.AppendLine($"{indent}{{");

            var seen = new HashSet<string>();
            foreach (string value in values)
            {
                string safe = ToSafeIdentifier(value);

                if (!seen.Add(safe))
                {
                    Debug.LogWarning($"[LocalisationTypeGenerator] Duplicate identifier \"{safe}\" (from \"{value}\") — skipped.");
                    continue;
                }

                string comment = safe != value ? $" // original: {value}" : "";
                sb.AppendLine($"{indent}    {safe},{comment}");
            }

            sb.AppendLine($"{indent}}}");

            if (hasNamespace)
                sb.AppendLine("}");

            return sb.ToString();
        }

        private static string ToSafeIdentifier(string value)
        {
            string safe = Regex.Replace(value, @"[^a-zA-Z0-9_]", "_");
            if (safe.Length > 0 && char.IsDigit(safe[0]))
                safe = "_" + safe;
            return safe;
        }

        private static void WriteFile(string path, string content)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, content, Encoding.UTF8);
        }
    }
}
#endif