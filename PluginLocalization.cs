using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using PotionCraft.Assemblies.DataBaseSystem.PreparedObjects;
using PotionCraft.LocalizationSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

// change this to the "default" namespace (visible in Project properties) or it won't work
namespace AutoBrew
{
    internal static class PluginLocalization
    {
        // change this to point to the plugin's ManualLogSource
        static ManualLogSource Log => AutoBrewPlugin.Log;

        static bool _initialised;

        public static readonly LocalizationManager.Locale DefaultLocale = LocalizationManager.Locale.en;
        public static LocalizationData library;
        public static readonly bool _parseLoose = true;
        public static readonly bool _parseDirectory = false;

        [HarmonyPostfix, HarmonyPatch(typeof(LocalizationManager), "LoadLocalizationData")]
        public static void LoadLocalizationData_Postfix(ref LocalizationData __result)
        {
            library = __result;
            ParseLocalizationData(true);
        }

        public static void ParseLocalizationData(bool forceInit)
        {
            if (_initialised && !forceInit)
            {
                return;
            }

            var thisAssembly = Assembly.GetExecutingAssembly();
            var manifestFiles = thisAssembly.GetManifestResourceNames();
            var basepath = Path.GetDirectoryName(thisAssembly.Location);

            foreach (LocalizationManager.Locale lang in Enum.GetValues(typeof(LocalizationManager.Locale)))
            {
                string json = null;
                string filename = $"{lang}.json";
                string embedpath = $"locales.{filename}";
                string localepath = Path.Combine("locales", filename);

                string[] matches = Array.FindAll(manifestFiles, (f => f.EndsWith(embedpath)));
                if ((matches != null) && (matches.Length != 0))
                {
                    Log.LogInfo($"ParseLocales: Parsing embedded resource '{localepath}'");
                    if (matches.Length > 1)
                    {
                        // do NOT embed multiple json files for the same locale
                        Log.LogWarning($"ParseLocales: Found multiple embedded files for '{lang}'. Using {matches[0]}");
                    }
                    using var resStream = thisAssembly.GetManifestResourceStream(matches[0]);
                    using var reader = new StreamReader(resStream, Encoding.UTF8);
                    json = reader.ReadToEnd();
                }
                else if (_parseLoose)
                {
                    string filepath = Path.Combine(basepath, localepath);
                    if (!File.Exists(filepath))
                    {
                        Log.LogDebug($"ParseLocales: Skipping '{localepath}' - file does not exist");
                        continue;
                    }
                    Log.LogInfo($"ParseLocales: Parsing loose resource '{localepath}'");
                    json = File.ReadAllText(filepath);
                }

                if (!string.IsNullOrEmpty(json))
                {
                    if (ParseJsonLocales(json, lang, out int counter))
                    {
                        Log.LogDebug($"ParseLocales: Added {counter} key entries for '{localepath}'");
                    }
                    else
                    {
                        Log.LogInfo($"Error encountered trying to parse '{localepath}'");
                    }
                }

                if (_parseDirectory)
                {
                    string langpath = Path.Combine("locales", lang.ToString());
                    string dirpath = Path.Combine(basepath, langpath);
                    if (Directory.Exists(dirpath))
                    {
                        foreach (var name in Directory.EnumerateFiles(dirpath, "*.json"))
                        {
                            filename = Path.GetFileName(name);
                            localepath = Path.Combine(langpath, filename);
                            Log.LogInfo($"ParseLocales: Parsing loose resource '{localepath}'");
                            json = File.ReadAllText(name);

                            if (!string.IsNullOrEmpty(json))
                            {
                                if (ParseJsonLocales(json, lang, out int counter))
                                {
                                    Log.LogDebug($"ParseLocales: Added {counter} key entries for '{localepath}'");
                                }
                                else
                                {
                                    Log.LogInfo($"Error encountered trying to parse '{localepath}'");
                                }
                            }
                        }
                    }
                }
                _initialised = true;
            }
        }

        private static bool ParseJsonLocales(string json, LocalizationManager.Locale lang, out int counter)
        {
            counter = 0;
            Dictionary<string, string> data;
            
            try
            {
                data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch (Exception e)
            {
                Log.LogError(e);
                return false;
            }

            foreach ((string key, string text) in data.Select(kvp => (kvp.Key, kvp.Value)))
            {
                if (library.Contains(key, (int)lang, (int)lang))
                {
                    Log.LogWarning($"Skipping '{key}' as it already exists for Locale.{lang}");
                    continue;
                }
                library.Add((int)lang, key, text);
                counter++;
            }

            return true;
        }

        public static bool KeyExists(string key, LocalizationManager.Locale lang)
        {
            if (library == null)
            {
                return false;
            }
            return library.Contains(key, (int)lang, (int)DefaultLocale);
        }

        public static string GetCustText(Key source)
        {
            if (KeyExists(source.key, LocalizationManager.currentLocale))
            {
                return source.GetText(LocalizationManager.currentLocale);
            }
            return GetDefText(source);
        }

        public static string GetDefText(Key source)
        {
            if (!KeyExists(source.key, DefaultLocale))
            {
                Log.LogError($"Key '{source.key} not found");
            }
            return source.GetText(DefaultLocale);
        }
    }
}
