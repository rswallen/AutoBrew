using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.PotionDescriptionWindow;
using UnityEngine;

namespace AutoBrew.UIElements
{
    internal static partial class UIUtilities
    {
        internal static InputFieldCanvas DescInputFieldCanvasTemplate
        {
            get
            {
                descInputFieldCanvasTemplate ??= GetDescInputFieldCanvas();
                return descInputFieldCanvasTemplate;
            }
        }
        private static InputFieldCanvas descInputFieldCanvasTemplate;

        public static InputFieldCanvas SpawnInputFieldCanvas()
        {
            if (!Clone(DescInputFieldCanvasTemplate, out var clone))
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

        private static InputFieldCanvas GetDescInputFieldCanvas()
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
                        var input = desc.GetComponentInChildren<InputFieldCanvas>();
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