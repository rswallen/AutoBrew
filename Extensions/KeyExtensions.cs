using PotionCraft.LocalizationSystem;
using QFSW.QC;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using static PotionCraft.LocalizationSystem.LocalizationManager;

namespace AutoBrew.Extensions
{
    internal static class KeyExtensions
    {
        public static bool Exists(this Key source)
        {
            return PluginLocalization.KeyExists(source.key, LocalizationManager.currentLocale);
        }

        public static bool Exists(this Key source, LocalizationManager.Locale locale)
        {
            return PluginLocalization.KeyExists(source.key, locale);
        }

        public static string GetCustText(this Key source)
        {
            return PluginLocalization.GetCustText(source);
        }

        public static string GetDefText(this Key source)
        {
            return PluginLocalization.GetDefText(source);
        }
    }
}
