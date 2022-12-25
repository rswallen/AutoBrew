using PotionCraft.LocalizationSystem;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.BrewControls
{
    internal class StepBrewButton : BrewButton
    {
        public static StepBrewButton Create(BrewControlsPanel controller)
        {
            var button = Create<StepBrewButton>(controller);

            button.label = UIUtilities.SpawnDescLocalizedText();
            button.label.transform.SetParent(button.transform, false);
            button.label.transform.localPosition = new(0f, 0f);
            button.label.text.text = "Step";

            button.spriteRenderer.sprite = UIUtilities.GetSpriteByName("Confirmation Ok Button");
            button.spriteRenderer.size = new(1.2f, 1.2f);

            (button.thisCollider as BoxCollider2D).size = button.spriteRenderer.size;

            button.FinalizeConstruction();
            return button;
        }

        private LocalizedText label;

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
        }
    }
}
