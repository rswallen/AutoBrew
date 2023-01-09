using AutoBrew.UIElements;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using Toolbar;
using UnityEngine;

namespace AutoBrew.Toolbar.Buttons
{
    internal class ToggleCookbookButton : BaseAutoBrewButton
    {
        public static ToggleCookbookButton Create(string buttonUID)
        {
            var button = ToolbarAPI.CreateCustomButton<ToggleCookbookButton>(buttonUID);

            var texture = TextureCache.LoadTexture("ToolbarIcons", "LoadPotionous.png");
            button.normalSprite = ToolbarUtils.MakeSprite("AutoBrew MainSubpanel ToggleCookbook", texture);

            button.spriteRenderer = ToolbarUtils.MakeRendererObj<SpriteRenderer>(button.GameObject, "Main Renderer", 100);
            button.spriteRenderer.sprite = button.normalSprite;

            button.IsActive = true;
            return button;
        }

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            UIManager.Cookbook?.Toggle();
        }

        public override TooltipContent GetTooltipContent()
        {
            return new TooltipContent()
            {
                header = "Review current recipe",
            };
        }
    }
}
