using AutoBrew.UIElements;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using Toolbar;
using UnityEngine;

namespace AutoBrew.Toolbar.Buttons
{
    internal sealed class ImportPotionousButton : BaseAutoBrewButton
    {
        public static ImportPotionousButton Create(string buttonUID)
        {
            var button = ToolbarAPI.CreateCustomButton<ImportPotionousButton>(buttonUID);

            var texture = TextureCache.LoadTexture("ToolbarIcons", "LoadPotionous.png");
            button.normalSprite = ToolbarUtils.MakeSprite("AutoBrew MainSubpanel LoadPotionous", texture);

            button.spriteRenderer = ToolbarUtils.MakeRendererObj<SpriteRenderer>(button.GameObject, "Main Renderer", 100);
            button.spriteRenderer.sprite = button.normalSprite;

            button.IsActive = true;
            return button;
        }

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            UIManager.Importer?.Toggle();
        }

        public override TooltipContent GetTooltipContent()
        {
            return new TooltipContent()
            {
                header = "Load from JSON\nPotionous URL or JSON",
            };
        }
    }
}