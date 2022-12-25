using PotionCraft.ObjectBased.UIElements.Tooltip;
using Toolbar;
using Toolbar.UIElements.Buttons;
using UnityEngine;

namespace AutoBrew.Toolbar.Buttons
{
    internal class MainSubPanelButton : SubPanelToolbarButton
    {
        public static T Create<T>(string buttonUID, string panelUID) where T : MainSubPanelButton
        {
            var button = ToolbarAPI.CreateCustomSubPanelButton<T>(buttonUID, panelUID);

            var mortarTexture = TextureCache.LoadTexture("ToolbarIcons", "Mortar Foreground Reduced.png");
            button.normalSprite = ToolbarUtils.MakeSprite("AutoBrew MainSubpanel Static", mortarTexture);
            
            button.spriteRenderer = ToolbarUtils.MakeRendererObj<SpriteRenderer>(button.GameObject, "Static Renderer", 110);
            button.spriteRenderer.sprite = button.normalSprite;
            button.spriteRenderer.transform.localPosition = new(0.0f, -0.1f);

            var optionTexture = TextureCache.FindTexture("RadialMenu OptionsIcon Active");
            button.normalSpriteIcon = ToolbarUtils.MakeSprite("AutoBrew MainSubpanel Spinner", optionTexture, 0.5f);
            
            button.spriteRendererIcon = ToolbarUtils.MakeRendererObj<SpriteRenderer>(button.GameObject, "Spinner Renderer", 100);
            button.spriteRendererIcon.sprite = button.normalSpriteIcon;
            button.spriteRendererIcon.transform.localPosition = new(0.0f, 0.1f);

            button.IsActive = true;
            return button;
        }

        private bool clockwise = true;

        public override void WhenButtonIsSelected()
        {
            base.WhenButtonIsSelected();
            float current = spriteRendererIcon.transform.localEulerAngles.z;
            current += Time.deltaTime * (clockwise ? -180f : 180f);
            spriteRendererIcon.transform.localEulerAngles = Vector3.forward * current;
        }

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            clockwise = !clockwise;
        }

        public override TooltipContent GetTooltipContent()
        {
            return new TooltipContent()
            {
                header = "AutoBrew",
            };
        }
    }
}