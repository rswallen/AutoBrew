using HarmonyLib;
using PotionCraft.ObjectBased.Bellows;
using PotionCraft.ObjectBased.InteractiveItem;
using PotionCraft.ObjectBased.InteractiveItem.InventoryObject;
using PotionCraft.ObjectBased.Ladle;
using PotionCraft.ObjectBased.Pestle;
using PotionCraft.ObjectBased.Salt;
using PotionCraft.ObjectBased.Spoon;
using PotionCraft.ObjectBased.Stack;
using UnityEngine;

namespace AutoBrew
{
    public sealed class Lockdown
    {
        private static double _gtLastWarning = 0.0;
        private static double _warnInterval = 2.0;

        // there has got to be a better method of doing this
        [HarmonyPostfix, HarmonyPatch(typeof(InteractiveItem), "CanBeInteractedNow")]
        public static void CanBeInteractedNow_Postfix(InteractiveItem __instance, ref bool __result)
        {
            // only override if game says item can be interacted with, or if we are brewing
            if (!__result || (!BrewMaster.Brewing))
            {
                return;
            }

            // only override user interaction with equipment and ingredients
            switch (__instance)
            {
                case Bellows:
                case Ladle:
                case Pestle:
                case Spoon:
                {
                    __result = !BrewMaster.Brewing;
                    DoNotTouch("Don't touch the equipment!");
                    return;
                }
                case InventoryObject:
                case SaltItem:
                case Stack:
                {
                    __result = !BrewMaster.Brewing;
                    DoNotTouch("Don't touch the ingredients!");
                    return;
                }
            }
        }

        public static void DoNotTouch(string message)
        {
            double gtNextWarning = _gtLastWarning + _warnInterval;
            if (Time.timeAsDouble >= gtNextWarning)
            {
                BrewMaster.PrintRecipeMapMessage(message, Vector2.zero);
                //Notification.ShowText(message, "Do not interfere with the brewing process!", Notification.TextType.EventText);
                _gtLastWarning = Time.timeAsDouble;
            }
        }
    }
}
