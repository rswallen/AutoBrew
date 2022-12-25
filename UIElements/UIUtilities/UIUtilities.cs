using BepInEx.Logging;
using PotionCraft.LocalizationSystem;
using System.Linq;
using TMPro;
using UnityEngine;

namespace AutoBrew.UIElements
{
    internal static partial class UIUtilities
    {
        internal static ManualLogSource Log => AutoBrewPlugin.Log;

        public static readonly string SortingLayerName = "DescriptionWindow";
        public static int SortingLayerID
        {
            get => SortingLayer.NameToID(SortingLayerName);
        }
        public static readonly Color TextColor = new(0.4118f, 0.2392f, 0.1725f, 1f);

        private static bool Clone<T>(T template, out T output) where T : MonoBehaviour
        {
            var template2 = template;
            if (template2 == null)
            {
                string name = typeof(T).Name;
                Log.LogError($"Could not find a {name} to clone. Impossible to create new {name}");
                output = null;
                return false;
            }

            output = Object.Instantiate(template2);
            output.name = typeof(T).Name;
            return true;
        }

        /// <summary>
        /// Helper function for creating renderer components.
        /// </summary>
        /// <typeparam name="T">Type of renderer to create (eg: SpriteRenderer, SpriteMask).</typeparam>
        /// <param name="parent">GameObject to attach the transform of the new gameObject to.</param>
        /// <param name="objName">Name of the new gameObject.</param>
        /// <param name="sortOrder">Value to set Renderer.sortingOrder to.</param>
        /// <returns>Reference to freshly create Renderer of type T.</returns>
        public static T MakeRendererObj<T>(GameObject parent, string objName, int sortOrder) where T : Renderer
        {
            GameObject obj = new()
            {
                name = objName,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(true);
            obj.transform.SetParent(parent.transform, false);
            var sr = obj.AddComponent<T>();
            sr.sortingLayerID = SortingLayerID;
            sr.sortingOrder = sortOrder;
            return sr;
        }

        public static T MakeRendererObj<T>(MonoBehaviour parent, string objName, int sortOrder) where T : Renderer
        {
            GameObject obj = new()
            {
                name = objName,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(true);
            obj.transform.SetParent(parent.transform, false);
            var sr = obj.AddComponent<T>();
            sr.sortingLayerID = SortingLayerID;
            sr.sortingOrder = sortOrder;
            return sr;
        }

        private static TMP_FontAsset[] fontAssets;

        public static LocalizedText MakeLocalizedTextObj(MonoBehaviour parent, string font, int sortingOrder)
        {
            fontAssets ??= Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            var fontAsset = fontAssets.FirstOrDefault(x => x.name.Equals(font, System.StringComparison.OrdinalIgnoreCase));
            if (fontAsset == null)
            {
                return null;
            }

            GameObject obj = new()
            {
                name = typeof(LocalizedText).Name,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(true);

            var tmp = obj.AddComponent<TextMeshPro>();
            tmp.font = fontAsset;
            tmp.color = TextColor;
            tmp.sortingLayerID = SortingLayerID;
            tmp.sortingOrder = sortingOrder;
            
            // needed so that the LocalizedText component doesn't freak out when it awakens
            tmp.text = "";
            var lt = obj.AddComponent<LocalizedText>();
            obj.transform.SetParent(parent.transform, false);
            return lt;
        }

        public static Sprite GetSpriteByName(string name)
        {
            return Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name.Equals(name));
        }
    }
}