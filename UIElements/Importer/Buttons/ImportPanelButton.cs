using PotionCraft.LocalizationSystem;
using PotionCraft.ObjectBased.UIElements;
using UnityEngine;

namespace AutoBrew.UIElements.Importer.Buttons
{
    internal class ImportPanelButton : SpriteChangingButton
    {
        public static T Create<T>(ImportPanel panel) where T : ImportPanelButton
        {
            // Make button GameObject
            GameObject obj = new()
            {
                name = typeof(T).Name,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);

            var button = obj.AddComponent<T>();

            button.thisCollider = obj.AddComponent<BoxCollider2D>();

            button.IgnoreRotationForPivot = true;
            button.showOnlyFingerWhenInteracting = true;
            button.raycastPriorityLevel = -21000;

            button.spriteRenderer = UIUtilities.MakeRendererObj<SpriteRenderer>(panel.gameObject, "Main Renderer", 100);
            button.spriteRenderer.transform.SetParent(button.transform, false);

            button.hoveredAlpha = 0.4f;
            button.pressedAlpha = 0.4f;
            button.normalAlpha = 0.3f;
            button.lockedAlpha = 0.15f;

            button.text = UIUtilities.SpawnDescLocalizedText();
            button.text.transform.SetParent(button.transform, false);
            button.text.transform.localPosition = new(0f, 0f);

            button.panel = panel;
            return button;
        }

        private protected ImportPanel panel;
        private protected LocalizedText text;
    }
}
