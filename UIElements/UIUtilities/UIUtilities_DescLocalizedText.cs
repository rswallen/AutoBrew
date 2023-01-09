using PotionCraft.LocalizationSystem;
using PotionCraft.ObjectBased.UIElements.PotionDescriptionWindow;
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
    }
}