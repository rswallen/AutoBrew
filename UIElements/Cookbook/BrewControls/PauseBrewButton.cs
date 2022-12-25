using PotionCraft.LocalizationSystem;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.BrewControls
{
    internal sealed class PauseBrewButton : BrewButton
    {
        public static PauseBrewButton Create(BrewControlsPanel controller)
        {
            var button = Create<PauseBrewButton>(controller);
            
            button.label = UIUtilities.SpawnDescLocalizedText();
            button.label.transform.SetParent(button.transform, false);
            button.label.transform.localPosition = new(0f, 0f);
            button.label.text.text = "Pause";

            button.bgPause = UIUtilities.GetSpriteByName("Confirmation Ok Button");
            button.bgPlay = UIUtilities.GetSpriteByName("Confirmation Yes Button");

            button.spriteRenderer.sprite = button.bgPause;
            button.spriteRenderer.size = new(2.5f, 1.2f);

            (button.thisCollider as BoxCollider2D).size = button.spriteRenderer.size;

            //button.spriteRendererIcon = UIUtilities.MakeRendererObj<SpriteRenderer>(button, "Icon Renderer", 110);

            button.FinalizeConstruction();
            return button;
        }

        private LocalizedText label;
        private Sprite bgPause;
        private Sprite bgPlay;
        private bool isPause = true;

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            if (isPause)
            {
                controller.PauseBrew();
                spriteRenderer.sprite = bgPlay;
                SetSprites(bgPlay);
                label.text.text = "Continue";
            }
            else
            {
                controller.ContinueBrew();
                spriteRenderer.sprite = bgPause;
                SetSprites(bgPause);
                label.text.text = "Pause";
            }
            isPause = !isPause;
        }

        private void SetSprites(Sprite img)
        {
            hoveredSprite = img;
            lockedSprite = img;
            normalSprite = img;
            pressedSprite = img;
        }
    }
}
