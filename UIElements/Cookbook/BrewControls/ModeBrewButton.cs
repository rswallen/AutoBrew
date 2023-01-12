using PotionCraft.LocalizationSystem;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.BrewControls
{
    internal class ModeBrewButton : BrewButton
    {
        public static ModeBrewButton Create(BrewControlsPanel controller)
        {
            var button = Create<ModeBrewButton>(controller);

            button.label = UIUtilities.SpawnDescLocalizedText();
            button.label.transform.SetParent(button.transform, false);
            button.label.transform.localPosition = new(0f, 0f);

            button.SetSprites(UIUtilities.GetSpriteByName("Confirmation Ok Button"));
            button.spriteRenderer.size = new(1.2f, 1.2f);

            (button.thisCollider as BoxCollider2D).size = button.spriteRenderer.size;

            button.FinalizeConstruction();
            return button;
        }

        private LocalizedText label;

        public override void Awake()
        {
            base.Awake();
            BrewMaster.OnModeChanged.AddListener(OnBrewModeChanged);
            OnBrewModeChanged(BrewMaster.Mode);
        }

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            BrewMaster.Mode = (BrewMaster.Mode == BrewMode.Continuous) ? BrewMode.StepForward : BrewMode.Continuous;
        }

        private void OnBrewModeChanged(BrewMode newMode)
        {
            switch (newMode)
            {
                case BrewMode.Continuous:
                {
                    label.text.text = "Do All";
                    return;
                }
                case BrewMode.StepForward:
                {
                    label.text.text = "Do 1";
                    return;
                }
            }
        }

        public override TooltipContent GetTooltipContent()
        {
            switch (BrewMaster.Mode)
            {
                case BrewMode.Continuous:
                {
                    return new TooltipContent()
                    {
                        header = "BrewMode: Continuous",
                        textBelowHeader = "Brewing will execute all instructions without pausing betweem instructions.",
                        textBelowSprite = "Click to change mode to Step By Step",
                    };
                }
                case BrewMode.StepForward:
                {
                    return new TooltipContent()
                    {
                        header = "BrewMode: Step By Step",
                        textBelowHeader = "Brewing will pause at the completion of each step, and will only restart once you press the Play/Pause button",
                        textBelowSprite = "Click to change mode to Continuous"
                    };
                }
            }
            return null;
        }
    }
}
