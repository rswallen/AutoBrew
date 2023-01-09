using BepInEx.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew
{
    public static class ABSettings
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        public static void LogConfigMissing(string source, string settingName)
        {
            Log.LogWarning($"{source}: Error parsing '{settingName}'. Either could not find or could not parse");
        }

        public static void LogConfigParseFail<T>(string source, string settingName, T defaultValue)
        {
            Log.LogWarning($"{source}: Error parsing '{settingName}'. Expected {typeof(T)}. Assigned default value of {defaultValue}");
        }

        public static bool GetBool(string source, Dictionary<string, string> repo, string key, out bool output, bool defaultValue = false, bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(source, key);
                }
                return false;
            }
            bool success = (bool.TryParse(repo[key], out output));
            if (!success && logFailure)
            {
                LogConfigParseFail<bool>(source, key, defaultValue);
            }
            return success;
        }

        public static bool GetInt(string source, Dictionary<string, string> repo, string key, out int output, int defaultValue = 0, bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(source, key);
                }
                return false;
            }
            bool success = (int.TryParse(repo[key], out output));
            if (!success && logFailure)
            {
                ABSettings.LogConfigParseFail<int>(source, key, defaultValue);
            }
            return success;
        }

        public static bool GetFloat(string source, Dictionary<string, string> repo, string key, out float output, float defaultValue = 0f, bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(source, key);
                }
                return false;
            }
            bool success = (float.TryParse(repo[key], out output));
            if (!success && logFailure)
            {
                LogConfigParseFail<float>(source, key, defaultValue);
            }
            return success;
        }

        public static bool GetDouble(string source, Dictionary<string, string> repo, string key, out double output, double defaultValue = 0f, bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(source, key);
                }
                return false;
            }
            bool success = (double.TryParse(repo[key], out output));
            if (!success && logFailure)
            {
                LogConfigParseFail<double>(source, key, defaultValue);
            }
            return success;
        }

        public static bool GetString(string source, Dictionary<string, string> repo, string key, out string output, string defaultValue = "", bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(source, key);
                }
                return false;
            }
            output = repo[key];
            return true;
        }

        public static bool GetVector2(string source, Dictionary<string, string> repo, string key, out Vector2 output, Vector2 defaultValue, bool logFailure = true)
        {
            output = defaultValue;
            if (!GetString(source, repo, key, out string value, "", logFailure))
            {
                return false;
            }

            var axes = value.Split(',');
            if (axes.Length != 2)
            {
                if (logFailure)
                {
                    LogConfigParseFail<Vector2>(source, key, defaultValue);
                }
                return false;
            }

            bool success = true;
            success &= float.TryParse(axes[0], out float x);
            success &= float.TryParse(axes[1], out float y);

            if (!success)
            {
                if (logFailure)
                {
                    LogConfigParseFail<Vector2>(source, key, defaultValue);
                }
                return false;
            }
            output = new Vector2(x, y);
            return true;
        }

        public static bool GetVector3(string source, Dictionary<string, string> repo, string key, out Vector3 output, Vector3 defaultValue, bool logFailure = true)
        {
            output = defaultValue;
            if (!GetString(source, repo, key, out string value, "", logFailure))
            {
                return false;
            }

            var axes = value.Split(',');
            if (axes.Length != 3)
            {
                if (logFailure)
                {
                    LogConfigParseFail<Vector3>(source, key, defaultValue);
                }
                return false;
            }

            bool success = true;
            success &= float.TryParse(axes[0], out float x);
            success &= float.TryParse(axes[1], out float y);
            success &= float.TryParse(axes[2], out float z);

            if (!success)
            {
                if (logFailure)
                {
                    LogConfigParseFail<Vector3>(source, key, defaultValue);
                }
                return false;
            }
            output = new Vector3(x, y, z);
            return true;
        }
    }
}