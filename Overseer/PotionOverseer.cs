﻿using HarmonyLib;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Salts;
using System.Collections.Generic;

namespace AutoBrew.Overseer
{
    internal static class PotionOverseer
    {
		[HarmonyPostfix, HarmonyPatch(typeof(PotionManager), "ResetPotion")]
        public static void ResetPotion_Postfix(bool resetEffectMapItems = true)
        {
            if (!BrewMaster.Initialised || !BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Abort("Potion failed");
		}

        [HarmonyPostfix, HarmonyPatch(typeof(PotionManager.RecipeMarksSubManager), "AddFloatMark")]
        public static void AddFloatMark_Postfix(List<SerializedRecipeMark> recipeMarksList, SerializedRecipeMark.Type type, float value, float multiplier, float maxValue)
		{
            if (!BrewMaster.Initialised || !BrewMaster.Brewing)
            {
                return;
            }
            switch (type)
			{
				case SerializedRecipeMark.Type.Spoon:
				{
					BrewMaster.Stirrer.AddSpoonAmount(value, multiplier);
					return;
				}
                case SerializedRecipeMark.Type.Ladle:
				{
                    BrewMaster.Pourer.AddLadleAmount(value, multiplier);
                    return;
				}
                default: return;
			}
		}

        [HarmonyPostfix, HarmonyPatch(typeof(PotionManager.RecipeMarksSubManager), "AddIngredientMark")]
        public static void AddIngredientMark_Postfix(List<SerializedRecipeMark> recipeMarksList, Ingredient ingredient, float grindStatus)
        {
            if (!BrewMaster.Initialised || !BrewMaster.Brewing)
			{
				return;
			}
			BrewMaster.Grinder.AddIngredientMark(ingredient, grindStatus);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PotionManager.RecipeMarksSubManager), "AddSaltMark")]
        public static void AddSaltMark_Postfix(List<SerializedRecipeMark> recipeMarksList, Salt salt, int amount = 1)
        {
            if (!BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Larder.AddSaltMark(recipeMarksList, salt);
        }
    }
}
