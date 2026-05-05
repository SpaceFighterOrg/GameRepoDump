namespace Assets.Scripts.Utils.I18N
{
    public static class LocalizationExtensions
    {
        public static string Localize(this string key)
            => LocalizationManager.Get(key);

        public static string Localize(this string key, Languages language)
            => LocalizationManager.GetIn(key, language);

        public static string Localize(this LocalisationKeys key)
            => LocalizationManager.Get(key.ToString());

        public static string Localize(this LocalisationKeys key, Languages language)
            => LocalizationManager.GetIn(key.ToString(), language);
    }
}
