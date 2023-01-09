using PotionCraft.Core.Extensions;
using PotionCraft.LocalizationSystem;
using PotionCraft.ObjectBased.InteractiveItem;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.RecipeChecklist
{
    internal class ChecklistItem : InteractiveItem
    {
        public static ChecklistItem Create()
        {
            GameObject obj = new()
            {
                name = typeof(ChecklistItem).Name,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);

            var item = obj.AddComponent<ChecklistItem>();
            item.locales = UIUtilities.SpawnDescLocalizedText();
            item.locales.transform.SetParent(item.transform, false);
            item.locales.transform.localPosition = new(0f, 0f);
            item.locales.OnTextUpdate.AddListener(item.OnTextUpdated);

            item.label = item.locales.text as TextMeshPro;
            item.label.text = "";
            item.label.alignment = TextAlignmentOptions.Left;

            var rectTransform = item.label.rectTransform;
            rectTransform.anchoredPosition = new(0.3f, -0.5f);
            rectTransform.pivot = new(0f, 0f);
            rectTransform.sizeDelta = new(5f, 1f);
            rectTransform.offsetMin = new(0.3f, -0.5f);
            rectTransform.offsetMax = new(5.3f, 0.5f);

            item.check = UIUtilities.MakeRendererObj<SpriteRenderer>(item, "Check Renderer", 100);

            item.thisCollider = obj.AddComponent<BoxCollider2D>();
            item.thisCollider.size = new(1f, 1f);

            // needed in order for tooltips to display
            item.tooltipContentProvider = obj.AddComponent<TooltipContentProvider>();
            item.tooltipContentProvider.fadingType = TooltipContentProvider.FadingType.UIElement;
            item.tooltipContentProvider.tooltipCollider = item.thisCollider;
            item.tooltipContentProvider.positioningSettings = new List<PositioningSettings>()
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

            obj.SetActive(true);
            return item;
        }

        private static void GetSprites()
        {
            tick = UIUtilities.GetSpriteByName("FollowButton Checkbox Ticked");
            idle = UIUtilities.GetSpriteByName("FollowButton Checkbox Idle");
        }

        private static Sprite tick;
        private static Sprite idle;

        private LocalizedText locales;
        private TextMeshPro label;
        private SpriteRenderer check;
        private BoxCollider2D thisCollider;

        public bool IsTicked
        {
            get { return isTicked; }
            set
            {
                SetTicked(value);
            }
        }
        private bool isTicked = false;

        private void SetTicked(bool ticked)
        {
            if ((tick == null) || (idle == null))
            {
                GetSprites();
            }

            if (ticked)
            {
                check.sprite = tick;
                check.SetColorAlpha(0.7f);
                label.color = new(0.4118f, 0.2392f, 0.1725f, 0.7f);
            }
            else
            {
                check.sprite = tick;
                check.SetColorAlpha(1f);
                label.color = new(0.4118f, 0.2392f, 0.1725f, 1f);
            }

            check.sprite = ticked ? tick : idle;
            isTicked = ticked;
        }

        public override void Awake()
        {
            base.Awake();
            SetTicked(false);
        }

        private void OnTextUpdated(string newText)
        {

        }

        public void SetText(string text)
        {
            label.text = text;
        }
    }
}
