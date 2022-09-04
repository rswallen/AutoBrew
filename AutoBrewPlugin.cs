using AutoBrew.Overseer;
using AutoBrew.UI;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using QFSW.QC;

namespace AutoBrew
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, "0.2.1")]
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
            Harmony.CreateAndPatchAll(typeof(UserInput));
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
    }
}