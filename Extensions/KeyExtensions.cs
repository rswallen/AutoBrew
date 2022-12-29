using PotionCraft.LocalizationSystem;

namespace AutoBrew.Extensions
{
    internal static class KeyExtensions
    {
        public static bool Exists(this Key source, LocalizationManager.Locale locale = LocalizationManager.Locale.en)
        {
            return PluginLocalization.KeyExists(source.key, locale);
        }

        public static string GetAutoBrewText(this Key source, LocalizationManager.Locale locale = LocalizationManager.Locale.en)
        {
            return PluginLocalization.GetAutoBrewText(source, locale);
        }
    }
}
