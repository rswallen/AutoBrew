using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Salts;
using System;
using System.Collections.Generic;
using AutoBrew.UIElements.Misc;

namespace AutoBrew.UIElements.Cookbook.Instructions
{
    internal sealed class InstructionEditor : BaseInstruction
    {
        public static InstructionEditor Create()
        {
            var item = Create<InstructionEditor>();

            item.type = Dropdown.Create();
            item.type.transform.SetParent(item.transform, false);
            item.type.transform.localPosition = new(0f, 0.5f);


            //item.item = UIUtilities.SpawnDropdown();
            //item.type.transform.SetParent(item.transform, false);
            //item.type.transform.localPosition = new(0f, -0.5f);

            item.IsActive = true;
            return item;
        }

        public void Awake()
        {
            type.AddOptions(new List<string>()
            {
                BrewOrderType.AddIngredient.ToString(),
                BrewOrderType.AddSalt.ToString(),
                BrewOrderType.StirCauldron.ToString(),
                BrewOrderType.PourSolvent.ToString(),
                BrewOrderType.HeatVortex.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
                BrewOrderType.AddEffect.ToString(),
            });
            //type.onValueChanged.AddListener(OnTypeValueChanged);
            //type.value = 0;
        }

        private static void AddIngredients(Dropdown drop)
        {
            //drop.ClearOptions();
            foreach (var item in Ingredient.allIngredients)
            {
                //drop.options.Add(new(item.GetLocalizedTitle()));
            }
        }

        private static void AddSalts(Dropdown drop)
        {
            //drop.ClearOptions();
            foreach (var item in Salt.allSalts)
            {
                //drop.options.Add(new(item.GetLocalizedTitle()));
               // drop.options.Add(new(item.GetLocalizedTitle()));
            }
        }

        private Dropdown type;
        private Dropdown item;

        /*
        public void OnTypeValueChanged(int newValue)
        {
            Enum.TryParse<BrewOrderType>(type.itemText.text, out var result);
            switch (result)
            {
                case BrewOrderType.AddIngredient:
                {
                    AddIngredients(item);
                    item.enabled = true;
                    break;
                }
                case BrewOrderType.AddSalt:
                {
                    AddSalts(item);
                    item.enabled = true;
                    break;
                }
                default:
                {
                    item.enabled = false;
                    break;
                }
            }
        }
        */
    }
}
