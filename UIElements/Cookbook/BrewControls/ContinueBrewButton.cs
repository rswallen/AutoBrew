using PotionCraft.LocalizationSystem;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.BrewControls
{
    internal sealed class ContinueBrewButton : BrewButton
    {
        public static ContinueBrewButton Create(BrewControlsPanel controller)
        {
            var button = Create<ContinueBrewButton>(controller);

            button.label = UIUtilities.SpawnDescLocalizedText();
            button.label.transform.SetParent(button.transform, false);
            button.label.transform.localPosition = new(0f, 0f);
            button.label.text.text = "Continue";

            button.SetSprites(UIUtilities.GetSpriteByName("Confirmation Yes Button"));
            button.spriteRenderer.size = new(2.5f, 1.2f);

            button.SetSprites(button.spriteRenderer.sprite);

            (button.thisCollider as BoxCollider2D).size = button.spriteRenderer.size;

            button.FinalizeConstruction();
            return button;
        }

        private LocalizedText label;

        public override void Awake()
        {
            base.Awake();
            Log.LogDebug($"ContinueBrewButton Awake at frame {Time.frameCount}");
        }

        public override void Start()
        {
            base.Start();
            Log.LogDebug($"ContinueBrewButton Start at frame {Time.frameCount}");
            BrewMaster.OnStateChanged.AddListener(OnBrewStateChanged);
            OnBrewStateChanged(BrewMaster.State);
        }

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            BrewMaster.State = BrewState.Brewing;
        }

        private void OnBrewStateChanged(BrewState newState)
        {
            switch (newState)
            {
                case BrewState.Brewing:
                {
                    Locked = true;
                    return;
                }
                case BrewState.Paused:
                {
                    Locked = false;
                    return;
                }
            }
        }
    }
}
