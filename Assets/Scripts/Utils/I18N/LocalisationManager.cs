using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utils.I18N
{
    public static class LocalizationManager
    {
        public static string CsvResourcePath = "i18n";
        public static string MissingKeyFallback = "[MISSING:{0}]";

        private static readonly Dictionary<string, string[]> Table =
            new(StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init() => LoadCSV();

        public static string Get(string key)
            => GetIn(key, SessionManager.Language);

        public static string GetIn(string key, Languages language)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[LocalizationManager] Get() called with a null/empty key.");
                return string.Format(MissingKeyFallback, "(empty)");
            }

            if (!Table.TryGetValue(key, out string[] values))
            {
                Debug.LogWarning($"[LocalizationManager] Key not found: \"{key}\"");
                return string.Format(MissingKeyFallback, key);
            }

            int col = (int)language;
            if (col >= values.Length || string.IsNullOrEmpty(values[col]))
            {
                Debug.LogWarning($"[LocalizationManager] No {language} text for key \"{key}\". Falling back to English.");
                return values.Length > 0 ? values[0] : string.Format(MissingKeyFallback, key);
            }

            return values[col];
        }

        public static void Reload()
        {
            Table.Clear();
            _initialized = false;
            LoadCSV();
        }

        public static bool HasKey(string key)
        {
            EnsureInitialized();
            return !string.IsNullOrEmpty(key) && Table.ContainsKey(key);
        }

        private static void EnsureInitialized()
        {
            if (!_initialized) LoadCSV();
        }

        private static void LoadCSV()
        {
            _initialized = true;

            TextAsset asset = Resources.Load<TextAsset>(CsvResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[LocalizationManager] Could not load CSV at Resources/{CsvResourcePath}.csv");
                return;
            }

            ParseCSV(asset.text);
            Debug.Log($"[LocalizationManager] Loaded {Table.Count} keys from '{CsvResourcePath}.csv'.");
        }

        private static void ParseCSV(string csvText)
        {
            csvText = csvText.Replace("\r\n", "\n").Replace('\r', '\n');

            var rows = new List<List<string>>();
            foreach (string line in csvText.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    rows.Add(SplitCSVLine(line));
            }

            if (rows.Count < 3)
            {
                Debug.LogError("[LocalizationManager] CSV must have at least 3 rows: keys, English, Bulgarian.");
                return;
            }

            List<string> keys = rows[0];
            List<string> english = rows[1];
            List<string> bulgarian = rows[2];

            for (int col = 0; col < keys.Count; col++)
            {
                string key = keys[col].Trim();
                if (string.IsNullOrEmpty(key)) continue;

                if (Table.ContainsKey(key))
                {
                    Debug.LogWarning($"[LocalizationManager] Duplicate key \"{key}\" — keeping first occurrence.");
                    continue;
                }

                string en = col < english.Count ? english[col].Trim() : string.Empty;
                string bg = col < bulgarian.Count ? bulgarian[col].Trim() : string.Empty;

                Table[key] = new[] { en, bg };
            }
        }

        private static List<string> SplitCSVLine(string line)
        {
            var fields = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
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
    }
}