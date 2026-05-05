using Assets.Scripts.Utils;
using Assets.Scripts.Utils.I18N;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts
{
    public static class PlanetTitleConverter
    {
        private static readonly Dictionary<Languages, Func<string, string>> _languageFunctionMap = new()
        {
            { Languages.Bulgarian, ToBulgarian },
            { Languages.English, ToEnglish }
        };

        public static string Translate(Languages languages, string title)
        {
            return _languageFunctionMap[languages](title);
        }

        public static string Translate(string title)
        {
            return Translate(SessionManager.Language, title);
        }

        public static string ToEnglish(string title)
        {
            if (string.IsNullOrEmpty(title))
                return title;

            var bgMap = new Dictionary<char, string>
            {
                { 'а', "a" }, { 'б', "b" }, { 'в', "v" }, { 'г', "g" },
                { 'д', "d" }, { 'е', "e" }, { 'ж', "zh" }, { 'з', "z" },
                { 'и', "i" }, { 'й', "j" }, { 'к', "k" }, { 'л', "l" },
                { 'м', "m" }, { 'н', "n" }, { 'о', "o" }, { 'п', "p" },
                { 'р', "r" }, { 'с', "s" }, { 'т', "t" }, { 'у', "u" },
                { 'ф', "f" }, { 'х', "h" }, { 'ц', "ts" }, { 'ч', "ch" },
                { 'ш', "sh" }, { 'щ', "sht" }, { 'ъ', "a" }, { 'ь', "" },
                { 'ю', "yu" }, { 'я', "ya" }
            };

            var sb = new StringBuilder();

            foreach (char c in title)
            {
                bool isUpper = char.IsUpper(c);
                char lower = char.ToLowerInvariant(c);

                if (bgMap.TryGetValue(lower, out string? en))
                {
                    if (isUpper && en.Length > 0)
                        sb.Append(char.ToUpperInvariant(en[0]) + en[1..]);
                    else
                        sb.Append(en);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static string ToBulgarian(string title)
        {
            if (string.IsNullOrEmpty(title))
                return title;

            var letterMap = new Dictionary<char, string>
            {
                { 'a', "а" }, { 'b', "б" }, { 'c', "к" }, { 'd', "д" },
                { 'e', "е" }, { 'f', "ф" }, { 'g', "г" }, { 'h', "х" },
                { 'i', "и" }, { 'j', "й" }, { 'k', "к" }, { 'l', "л" },
                { 'm', "м" }, { 'n', "н" }, { 'o', "о" }, { 'p', "п" },
                { 'r', "р" }, { 's', "с" }, { 't', "т" }, { 'u', "у" },
                { 'v', "в" }, { 'z', "з" }
            };

            var sb = new StringBuilder();
            int i = 0;
            string lower = title.ToLowerInvariant();

            while (i < lower.Length)
            {
                bool isUpper = char.IsUpper(title[i]);

                if (i + 2 < lower.Length && lower[i] == 's' && lower[i + 1] == 'h' && lower[i + 2] == 't')
                {
                    sb.Append(isUpper ? "Щ" : "щ");
                    i += 3;
                }
                else if (i + 1 < lower.Length && lower[i] == 'c' && lower[i + 1] == 'h')
                {
                    sb.Append(isUpper ? "Ч" : "ч");
                    i += 2;
                }
                else if (i + 1 < lower.Length && lower[i] == 's' && lower[i + 1] == 'h')
                {
                    sb.Append(isUpper ? "Ш" : "ш");
                    i += 2;
                }
                else if (i + 1 < lower.Length && lower[i] == 'z' && lower[i + 1] == 'h')
                {
                    sb.Append(isUpper ? "Ж" : "ж");
                    i += 2;
                }
                else if (letterMap.TryGetValue(lower[i], out string? bg))
                {
                    sb.Append(isUpper ? bg.ToUpperInvariant() : bg);
                    i++;
                }
                else
                {
                    sb.Append(title[i]);
                    i++;
                }
            }
            sb[0] = sb[0].ToString().ToUpper()[0];
            return sb.ToString();
        }
    }
}