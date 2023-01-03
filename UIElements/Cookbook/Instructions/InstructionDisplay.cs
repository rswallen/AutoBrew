using AutoBrew.UIElements.Misc;
using PotionCraft.LocalizationSystem;
using PotionCraft.ScriptableObjects.Ingredient;
using TMPro;
using Unity.Audio;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.Instructions
{
    internal sealed class InstructionDisplay : BaseInstruction
    {
        public static InstructionDisplay Create()
        {
            var item = Create<InstructionDisplay>();

            GameObject obj = new()
            {
                name = typeof(MoveUIHandle).Name,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(true);
            item.Anchor = obj.transform;
            item.transform.SetParent(item.Anchor, false);

            var symbol = obj.AddComponent<SpriteRenderer>();
            symbol.sprite = MoveUIHandle.SymbolIcon; ;
            symbol.drawMode = SpriteDrawMode.Sliced;
            symbol.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            symbol.size = new(0.3f, 0.3f);
            symbol.sortingLayerID = SortingLayer.NameToID("DescriptionWindow");
            symbol.sortingOrder = 5000;

            item.icon = UIUtilities.MakeRendererObj<SpriteRenderer>(item, "Icon Renderer", 150);
            item.icon.transform.localPosition = new(0.8f, 0f);
            item.icon.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            item.icon.drawMode = SpriteDrawMode.Sliced;

            item.Locales = UIUtilities.MakeLocalizedTmpObj("Vollkorn-PC Bold SDF");
            item.Locales.transform.SetParent(item.transform, false);
            item.Locales.transform.localPosition = new(0f, 0f);
            //item.localizedText.SetText(localeKey);

            item.Text = item.Locales.text as TextMeshPro;
            item.Text.transform.localPosition = new(4.2f, 0f);
            item.Text.rectTransform.sizeDelta = new(5f, 1f);
            item.Text.alignment = TextAlignmentOptions.Left;
            item.Text.enableWordWrapping = true;
            item.Text.fontSize = 3f;
            item.Text.sortingLayerID = SortingLayer.NameToID("DescriptionWindow");
            item.Text.sortingOrder = 150;
            
            item.IsActive = true;
            return item;
        }

        private static Sprite bellows;
        private static Sprite spoon;
        private static Sprite mortar;
        private static Sprite ladle;

        public static void UpdateSprites(int ppu)
        {
            var texture = TextureCache.FindTexture("Shop Upgrades Bellows");
            if (bellows != null)
            {
                Destroy(bellows);
            }
            bellows = Sprite.Create(texture, new(84f, 10f, 270f, 270f), new(0.5f, 0.5f), 200, 0, SpriteMeshType.FullRect, new(0f, 0f, 0f, 0f));

            texture = TextureCache.FindTexture("Shop Upgrades Spoon");
            if (spoon != null)
            {
                Destroy(spoon);
            }
            spoon = Sprite.Create(texture, new(149f, 72f, 140f, 140f), new(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new(0f, 0f, 0f, 0f));

            texture = TextureCache.FindTexture("Shop Upgrades Mortar");
            if (mortar != null)
            {
                Destroy(mortar);
            }
            mortar = Sprite.Create(texture, new(84f, 7f, 270f, 270f), new(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, new(0f, 0f, 0f, 0f));

            texture = TextureCache.FindTexture("Shop Upgrades Ladle");
            if (ladle != null)
            {
                Destroy(ladle);
            }
            ladle = Sprite.Create(texture, new(84f, 7f, 270f, 270f), new(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, new(0f, 0f, 0f, 0f));
        }

        public LocalizedText Locales;
        public TextMeshPro Text;
        public SpriteRenderer icon;

        public override void UpdateVisibility(bool newValue)
        {
            base.UpdateVisibility(newValue);
            Text.gameObject.SetActive(newValue);
        }

        public void ApplyOrder(BrewOrder order)
        {
            switch (order.Stage)
            {
                case BrewOrderType.AddIngredient:
                {
                    icon.sprite = mortar;
                    string ingredient = (order.Item as Ingredient).GetLocalizedTitle();
                    string text = $"Add 1 {ingredient}";
                    if (order.Target > 0f)
                    {
                        text += $", {order.Target * 100f}% ground";
                    }
                    else
                    {
                        text += $", unground";
                    }
                    Text.text = text;
                    break;
                }
                case BrewOrderType.StirCauldron:
                {
                    icon.sprite = spoon;
                    Text.text = $"Stir the cauldron {order.Target} times (TODO: figure out scalar)";
                    break;
                }
                case BrewOrderType.PourSolvent:
                {
                    icon.sprite = ladle;
                    Text.text = $"Add {order.Target} units of solvent";
                    break;
                }
                case BrewOrderType.HeatVortex:
                {
                    icon.sprite = bellows;
                    Text.text = $"Apply {order.Target} units of heat (TODO: what are these units?)";
                    break;
                }
                default:
                {
                    icon.sprite = bellows;
                    Text.text = $"This order is not configured: {order.Stage}";
                    break;
                }
            }
            icon.size = new(1.35f, 1.35f);
        }
    }
}