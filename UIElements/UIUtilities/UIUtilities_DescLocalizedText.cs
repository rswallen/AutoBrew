using PotionCraft.LocalizationSystem;
using PotionCraft.ObjectBased.UIElements.PotionDescriptionWindow;
using TMPro;
using UnityEngine;

namespace AutoBrew.UIElements
{
    internal static partial class UIUtilities
    {
        internal static LocalizedText DescLocalizedText
        {
            get
            {
                descLocalizedText ??= GetDescLocalizedText();
                return descLocalizedText;
            }
        }
        private static LocalizedText descLocalizedText;

        public static LocalizedText SpawnDescLocalizedText()
        {
            if (!Clone(DescLocalizedText, out var clone))
            {
                return null;
            }

            var renderers = clone.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.sortingLayerID = SortingLayerID;
            }

            clone.gameObject.SetActive(true);
            return clone;
        }

        private static LocalizedText GetDescLocalizedText()
        {
            var descs = Resources.FindObjectsOfTypeAll<DescriptionWindow>();
            if ((descs?.Length ?? 0) == 0)
            {
                return null;
            }

            foreach (var desc in descs)
            {
                if (desc != null)
                {
                    if (!desc.isActiveAndEnabled)
                    {
                        var input = desc.GetComponentInChildren<LocalizedText>();
                        if (input != null)
                        {
                            return input;
                        }
                    }
                }
            }
            return null;
        }

        public static LocalizedText SpawnLocalizedText(string font, string sortingGroup)
        {
            GameObject obj = new()
            {
                name = typeof(LocalizedText).Name,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);
            obj.AddComponent<RectTransform>();
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sortingLayerID = SortingLayer.NameToID(sortingGroup);
            
            obj.AddComponent<TextMeshPro>();

            var lt = obj.AddComponent<LocalizedText>();

            return lt;
        }
    }
}