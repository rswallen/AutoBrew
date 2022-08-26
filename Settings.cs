using BepInEx.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew
{
    public static class ABSettings
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        private static string _origin = "Settings";

        public static void SetOrigin(string origin)
        {
            if ((origin != null) && (origin != ""))
            {
                _origin = origin;
            }
        }

        public static void LogConfigMissing(string settingName)
        {
            if ((_origin == null) || (_origin == ""))
            {
                _origin = "Settings";
            }
            Log.LogWarning($"{_origin}: Error parsing '{settingName}'. Either could not find or could not parse");
        }

        public static void LogConfigParseFail<T>(string settingName, T defaultValue)
        {
            string expected = typeof(T).ToString();
            Log.LogWarning($"{_origin}: Error parsing '{settingName}'. Expected {expected}. Assigned default value of {defaultValue}");

        }

        public static bool GetBool(Dictionary<string, string> repo, string key, out bool output, bool defaultValue = false, bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(key);
                }
                return false;
            }
            bool success = (bool.TryParse(repo[key], out output));
            if (!success && logFailure)
            {
                LogConfigParseFail<bool>(key, defaultValue);
            }
            return success;
        }
        
        public static bool GetInt(Dictionary<string, string> repo, string key, out int output, int defaultValue = 0, bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(key);
                }
                return false;
            }
            bool success = (int.TryParse(repo[key], out output));
            if (!success && logFailure)
            {
                ABSettings.LogConfigParseFail<int>(key, defaultValue);
            }
            return success;
        }
        
        public static bool GetFloat(Dictionary<string, string> repo, string key, out float output, float defaultValue = 0f, bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(key);
                }
                return false;
            }
            bool success = (float.TryParse(repo[key], out output));
            if (!success && logFailure)
            {
                LogConfigParseFail<float>(key, defaultValue);
            }
            return success;
        }

        public static bool GetDouble(Dictionary<string, string> repo, string key, out double output, double defaultValue = 0f, bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(key);
                }
                return false;
            }
            bool success = (double.TryParse(repo[key], out output));
            if (!success && logFailure)
            {
                LogConfigParseFail<double>(key, defaultValue);
            }
            return success;
        }

        public static bool GetString(Dictionary<string, string> repo, string key, out string output, string defaultValue = "", bool logFailure = true)
        {
            output = defaultValue;
            if (!repo.ContainsKey(key))
            {
                if (logFailure)
                {
                    LogConfigMissing(key);
                }
                return false;
            }
            output = repo[key];
            return true;
        }

        public static bool GetVector2(Dictionary<string, string> repo, string key, out Vector2 output, Vector2 defaultValue, bool logFailure = true)
        {
            output = defaultValue;
            if (!GetString(repo, key, out string value, "", logFailure))
            {
                return false;
            }

            var axes = value.Split(',');
            if (axes.Length != 2)
            {
                if (logFailure)
                {
                    LogConfigParseFail<Vector2>(key, defaultValue);
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
                    LogConfigParseFail<Vector2>(key, defaultValue);
                }
                return false;
            }
            output = new Vector2(x, y);
            return true;
        }

        public static bool GetVector3(Dictionary<string, string> repo, string key, out Vector3 output, Vector3 defaultValue, bool logFailure = true)
        {
            output = defaultValue;
            if (!GetString(repo, key, out string value, "", logFailure))
            {
                return false;
            }

            var axes = value.Split(',');
            if (axes.Length != 3)
            {
                if (logFailure)
                {
                    LogConfigParseFail<Vector2>(key, defaultValue);
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
                    LogConfigParseFail<Vector3>(key, defaultValue);
                }
                return false;
            }
            output = new Vector3(x, y, z);
            return true;
        }
    }
}