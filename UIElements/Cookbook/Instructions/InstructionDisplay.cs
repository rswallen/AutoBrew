using PotionCraft.LocalizationSystem;
using TMPro;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.Instructions
{
    internal sealed class InstructionDisplay : BaseInstruction
    {
        public static InstructionDisplay Create(int testnum)
        {
            var item = Create<InstructionDisplay>();

            item.Locales = UIUtilities.SpawnDescLocalizedText();
            item.Locales.transform.SetParent(item.transform, false);
            item.Locales.transform.localPosition = new(0f, 0f);
            //item.localizedText.SetText(localeKey);

            item.Text = item.Locales.text as TextMeshPro;
            item.Locales.text.fontSize = 3f;
            //option.localizedText.text.overflowMode = TextOverflowModes.Ellipsis;

            item.icon = UIUtilities.MakeRendererObj<SpriteRenderer>(item, "Icon Renderer", 150);
            item.icon.transform.localPosition = new(-3f, 0f);
            item.icon.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            switch (testnum)
            {
                case 1:
                {
                    item.icon.sprite = UIUtilities.GetSpriteByName("RecipeBook RecipeIcon Spoon");
                    break;
                }
                case 2:
                {
                    item.icon.sprite = UIUtilities.GetSpriteByName("RecipeBook RecipeIcon Bellows");
                    break;
                }
                default:
                {
                    item.icon.sprite = UIUtilities.GetSpriteByName("RecipeBook RecipeIcon Ladle");
                    break;
                }
            }

            item.IsActive = true;
            return item;

            // cauldron 
            // Haggle ThemeIcon Alchemy 2 Large
            // Haggle ThemeIcon Alchemy 2 Medium

            // bellows
            // Haggle ThemeIcon Alchemy 0 Medium

            // mortar
            // Haggle ThemeIcon Alchemy TabIcon Active
            // Haggle ThemeIcon Alchemy 1 Medium
            // Haggle ThemeIcon Alchemy 1 Large

            // upgrades 84 84 270 150

        }

        public LocalizedText Locales;
        public TextMeshPro Text;
        private SpriteRenderer icon;

        private Sprite bellows;
        private Sprite mortar;
        private Sprite spoon;
        private Sprite ladle;

        public override void UpdateVisibility(bool newValue)
        {
            base.UpdateVisibility(newValue);
            Text.gameObject.SetActive(newValue);
            icon.enabled = newValue;
        }

        private void MakeSprites()
        {
            var texture = TextureCache.FindTexture("Shop Upgrades Bellows");
            bellows = Sprite.Create(texture, new(84f, 84f, 270f, 150f), new(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new(0f, 0f, 0f, 0f));

            texture = TextureCache.FindTexture("Shop Upgrades Spoon");
            spoon = Sprite.Create(texture, new(84f, 84f, 270f, 150f), new(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new(0f, 0f, 0f, 0f));

            texture = TextureCache.FindTexture("Shop Upgrades Mortar");
            mortar = Sprite.Create(texture, new(84f, 84f, 270f, 150f), new(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new(0f, 0f, 0f, 0f));

            texture = TextureCache.FindTexture("Shop Upgrades Ladle");
            ladle = Sprite.Create(texture, new(84f, 84f, 270f, 150f), new(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new(0f, 0f, 0f, 0f));
        }
    }
}
