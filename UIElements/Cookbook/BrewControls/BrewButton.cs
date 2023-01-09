using BepInEx.Logging;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.BrewControls
{
    internal abstract class BrewButton : SpriteChangingButton
    {
        private protected static ManualLogSource Log => AutoBrewPlugin.Log;

        public static T Create<T>(BrewControlsPanel controller) where T : BrewButton
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

            button.spriteRenderer = UIUtilities.MakeRendererObj<SpriteRenderer>(button, "Background Renderer", 100);
            button.spriteRenderer.drawMode = UnityEngine.SpriteDrawMode.Sliced;

            button.hoveredAlpha = 0.4f;
            button.pressedAlpha = 0.4f;
            button.lockedAlpha = 0.15f;
            button.normalAlpha = 0.3f;

            // needed in order for tooltips to display
            button.tooltipContentProvider = obj.AddComponent<TooltipContentProvider>();
            button.tooltipContentProvider.fadingType = TooltipContentProvider.FadingType.UIElement;
            button.tooltipContentProvider.tooltipCollider = button.thisCollider;
            button.tooltipContentProvider.positioningSettings = new List<PositioningSettings>()
            {
                new PositioningSettings()
                {
                    bindingPoint = PositioningSettings.BindingPoint.TransformPosition,
                    freezeX = false,
                    freezeY = true,
                    position = new Vector2(0.45f, 0.4f),
                    tooltipCorner = PositioningSettings.TooltipCorner.LeftTop,
                }
            };

            button.controller = controller;

            button.IgnoreRotationForPivot = true;
            button.showOnlyFingerWhenInteracting = true;
            button.raycastPriorityLevel = -13000;

            return button;
        }

        private protected void FinalizeConstruction()
        {
            IsActive = true;
            transform.SetParent(controller.transform, false);
        }

        private protected BrewControlsPanel controller;

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (isActive != value)
                {
                    isActive = value;
                    gameObject.SetActive(value);
                }
            }
        }
        private bool isActive = false;

        public override void Awake()
        {
            base.Awake();
            isActive = false;
            gameObject.SetActive(false);
        }
    }
}
