using PotionCraft.LocalizationSystem;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.BrewControls
{
    internal class AbortBrewButton : BrewButton
    {
        public static AbortBrewButton Create(BrewControlsPanel controller)
        {
            var button = Create<AbortBrewButton>(controller);

            button.label = UIUtilities.SpawnDescLocalizedText();
            button.label.transform.SetParent(button.transform, false);
            button.label.transform.localPosition = new(0f, 0f);
            button.label.text.text = "Abort";

            button.SetSprites(UIUtilities.GetSpriteByName("Confirmation No Button"));
            button.spriteRenderer.size = new(1.2f, 1.2f);

            (button.thisCollider as BoxCollider2D).size = button.spriteRenderer.size;

            //button.spriteRendererIcon = UIUtilities.MakeRendererObj<SpriteRenderer>(button, "Icon Renderer", 110);

            button.FinalizeConstruction();
            return button;
        }

        private LocalizedText label;

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            controller.ShowButtons(false);
            BrewMaster.Abort(null);
        }
    }
}
