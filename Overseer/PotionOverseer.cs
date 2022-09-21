using HarmonyLib;
using PotionCraft.LocalizationSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Salts;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal static class PotionOverseer
    {
        private static readonly Key _potionFailed = new("autobrew_brew_abort_potionfail");

        // MapLoader.SelectMapIfNotSelected does not use forced, so made a version that does
        public static void SelectMapAndLock(PotionBase potionBase, bool forced = false)
        {
            int num = Mathf.Max(0, MapLoader.loadedMaps.FindIndex((MapState mapState) => mapState.potionBase.name.Equals(potionBase.name)));
            if (Managers.RecipeMap.currentMap.index != num)
            {
                MapLoader.SelectMap(num, forced);
            }
            MapLoader.MapChangeLock = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PotionManager), "ResetPotion")]
        public static void ResetPotion_Postfix(bool resetEffectMapItems = true)
        {
            if (!BrewMaster.Initialised || !BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Abort(_potionFailed);
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

            BrewMaster.GetCurrentInstruction(out var order);
            switch (order.Stage)
            {
                case BrewStage.AddIngredient:
                {
                    BrewMaster.Larder.AddIngredientMark(ingredient, grindStatus);
                    break;
                }
                case BrewStage.GrindPercent:
                {
                    BrewMaster.Grinder.AddIngredientMark(ingredient, grindStatus);
                    break;
                }
            }
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
