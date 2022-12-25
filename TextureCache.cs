using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Toolbar;
using UnityEngine;

namespace AutoBrew
{
    internal static class TextureCache
    {
        private static Dictionary<string, Texture2D> textures = new();

        public static Texture2D LoadTexture(string subDirectory, string name)
        {
            if (!textures.ContainsKey(name) || textures[name] == null)
            {
                string assPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string iconPath = Path.Combine(assPath, subDirectory, name);

                ToolbarUtils.LoadTextureFromFile(iconPath, out Texture2D iconTexture);
                textures[name] = iconTexture;
            }
            return textures[name];
        }

        public static Texture2D FindTexture(string name)
        {
            if (!textures.ContainsKey(name) || textures[name] == null)
            {
                textures[name] = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x.name.Equals(name, System.StringComparison.Ordinal));
            }
            return textures[name];
        }

        public static Texture2D GetActiveIndicator()
        {
            if (!textures.ContainsKey("*Active") || textures["*Active"] == null)
            {
                if (!GetIndicators())
                {
                    return null;
                }
            }
            return textures["*Active"];
        }

        public static Texture2D GetInactiveIndicator()
        {
            if (!textures.ContainsKey("*Inactive") || textures["*Inactive"] == null)
            {
                if (!GetIndicators())
                {
                    return null;
                }
            }
            return textures["*Inactive"];
        }

        private static bool GetIndicators()
        {
            // brown rhombus: Alchemist'sPathBook FollowIcon Default|AlwaysFollow
            // white rhombus: GoalsTrackPanel FollowIcon Default|Follow
            var indicators = Resources.FindObjectsOfTypeAll<Texture2D>().Where(x => x.name.StartsWith("GoalsTrackPanel FollowIcon")).ToList();
            bool foundActive = false;
            bool foundInactive = false;
            foreach (var texture in indicators)
            {
                if (texture.name.EndsWith("Follow"))
                {
                    textures["*Active"] = texture;
                    foundActive = true;
                    continue;
                }
                if (texture.name.EndsWith("Default"))
                {
                    textures["*Inactive"] = texture;
                    foundInactive = true;
                    continue;
                }
            }

            return foundActive && foundInactive;
        }
    }
}
