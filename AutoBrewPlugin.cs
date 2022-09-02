using AutoBrew.Overseer;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.PotionCustomizationPanel;
using QFSW.QC;
using UnityEngine;

namespace AutoBrew
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, "0.1.0")]
    public class AutoBrewPlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        public void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Log = Logger;

            BrewMaster.Awake();

            Harmony.CreateAndPatchAll(typeof(PotionOverseer));
            Harmony.CreateAndPatchAll(typeof(SolventOverseer));
            Harmony.CreateAndPatchAll(typeof(BellowsOverseer));
            Harmony.CreateAndPatchAll(typeof(CauldronOverseer));
            Harmony.CreateAndPatchAll(typeof(InventoryOverseer));
            Harmony.CreateAndPatchAll(typeof(Lockdown));
            Harmony.CreateAndPatchAll(typeof(BrewMaster));
            Harmony.CreateAndPatchAll(typeof(AutoBrewPlugin));
        }

        public void Update()
        {
            BrewMaster.Update();
        }

        [Command("Autobrew-Reconfigure", "Attempts to load new settings from json file in the plugin folder", true, true, Platform.AllPlatforms, MonoTargetType.Single)]
        public static void Cmd_Reconfigure()
        {
            BrewMaster.LoadSettings();
        }

        [Command("Autobrew-Abort", "Aborts the current brew", true, true, Platform.AllPlatforms, MonoTargetType.Single)]
        public static void Cmd_Abort()
        {
            BrewMaster.Abort("You aborted the brew via command");
        }

        [Command("Autobrew-LogStatus", "Prints the status of the current order to the BepInEx console", true, true, Platform.AllPlatforms, MonoTargetType.Single)]
        public static void Cmd_LogStatus()
        {
            BrewMaster.LogCurrentStageProgress();
        }

        [Command("Autobrew-LogIndicatorRot", "Logs the rotation of the potion indicator", true, true, Platform.AllPlatforms, MonoTargetType.Single)]
        public static void Cmd_LogIndicatorRot()
        {
            Vector2 position = Managers.RecipeMap.recipeMapObject.indicatorContainer.localPosition;
            float rotation = Managers.RecipeMap.indicatorRotation.VisualValue;
            Log.LogInfo($"Indicator Position: ({position.x}|{position.y})");
            Log.LogInfo($"Indicator Rotation: {rotation}");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PotionCustomizationPanel), "OnPanelContainerStart")]
        public static void OnPanelContainerStart_Postfix(PotionCustomizationPanel __instance)
        {
            if (__instance.titleInputField != null)
            {
                __instance.titleInputField.onSubmit.AddListener(delegate (string text)
                {
                    if (BrewMaster.Brewing)
                    {
                        return;
                    }

                    if ((text.Length == 8) && text.Equals("AutoBrew"))
                    {
                        BrewMaster.InitBrew();
                    }
                });
            }
        }
    }
}