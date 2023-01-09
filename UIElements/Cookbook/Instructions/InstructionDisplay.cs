using PotionCraft.LocalizationSystem;
using PotionCraft.ScriptableObjects.Ingredient;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoBrew.UIElements.Cookbook.Instructions
{
    internal sealed class InstructionDisplay : BaseInstruction
    {
        public static InstructionDisplay Create()
        {
            var item = Create<InstructionDisplay>();

            item.Handle = InstructionHandle.Create();
            item.Handle.Instruction = item;
            item.Anchor = item.Handle.transform;
            item.transform.SetParent(item.Anchor, false);

            //item.icon = UIUtilities.MakeRendererObj<SpriteRenderer>(item, "Icon Renderer", idleSO);
            item.icon = UIUtilities.MakeCanvasSpriteObj(item, "Icon Renderer");
            item.icon.transform.localPosition = new(0.8f, 0f);
            item.icon.transform.localScale = new(0.0135f, 0.0135f);

            item.Text = UIUtilities.MakeTMPTextObj<TextMeshProUGUI>("Caveat-Bold SDF");
            item.Text.transform.SetParent(item.transform, false);
            item.Text.transform.localPosition = new(4.2f, 0f);
            item.Text.transform.localScale = new(0.1f, 0.1f);
            item.Text.rectTransform.sizeDelta = new(50f, 10f);
            item.Text.alignment = TextAlignmentOptions.Left;
            item.Text.enableWordWrapping = true;
            item.Text.fontSize = 3f;
            item.Text.text = "";

            item.Locales = item.Text.gameObject.AddComponent<LocalizedText>();
            //item.localizedText.SetText(localeKey);

            item.indexText.transform.SetAsLastSibling();
            item.Handle.IsActive = true;
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
        public TMP_Text Text;
        public Image icon;
        public InstructionHandle Handle;

        public override void UpdateVisibility(bool newValue)
        {
            base.UpdateVisibility(newValue);
            //Text.gameObject.SetActive(newValue);
        }

        public override void Apply(BrewOrder order)
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
            //icon.size = new(1.35f, 1.35f);
        }

        public override bool IsValid()
        {
            return true;
        }

        public override Bounds GetBounds()
        {
            var bounds = Text.GetTextBounds();
            //bounds.Encapsulate(icon.rectTransform.bounds);
            return bounds;
        }

        public override void OnAnchorGrabbed()
        {
            transform.SetAsLastSibling();
        }

        public override void OnAnchorReleased()
        {
            Parent.Refill(false);
        }
    }
}