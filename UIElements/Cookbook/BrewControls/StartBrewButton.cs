using PotionCraft.LocalizationSystem;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.BrewControls
{
    internal sealed class StartBrewButton : BrewButton
    {
        public static StartBrewButton Create(BrewControlsPanel controller)
        {
            var button = Create<StartBrewButton>(controller);

            button.label = UIUtilities.SpawnDescLocalizedText();
            button.label.transform.SetParent(button.transform, false);
            button.label.transform.localPosition = new(0f, 0f);
            button.label.text.text = "Start\nBrewing";
            
            button.SetSprites(UIUtilities.GetSpriteByName("Confirmation Yes Button"));
            button.spriteRenderer.size = new(2.5f, 2.5f);

            (button.thisCollider as BoxCollider2D).size = button.spriteRenderer.size;

            button.FinalizeConstruction();
            return button;
        }

        private LocalizedText label;

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            controller.ShowButtons(true);
            controller.Cookbook.StartBrewing();
        }
    }
}