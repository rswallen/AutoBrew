using AutoBrew.Overseer;
using AutoBrew.Toolbar;
using AutoBrew.UI;
using AutoBrew.UIElements;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PotionCraft.LocalizationSystem;
using QFSW.QC;
using Toolbar;

namespace AutoBrew
{
    [BepInDependency("com.github.rswallen.potioncraft.toolbar", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, "0.2.1")]
    public class AutoBrewPlugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        private static readonly Key _abortCommand = new("autobrew_brew_abort_command");

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
            Harmony.CreateAndPatchAll(typeof(PluginLocalization));
            Harmony.CreateAndPatchAll(typeof(UserInput));

            Harmony.CreateAndPatchAll(typeof(UIManager));

            ToolbarAPI.RegisterInit(ToolbarInterface.Setup);
        }

        public void Start()
        {
            BrewMaster.Start();
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

        [Command("Autobrew-ReloadLocales", "Manually trigger ParseLocalizationData", true, true, Platform.AllPlatforms, MonoTargetType.Single)]
        public static void Cmd_ReloadLocales()
        {
            PluginLocalization.ParseLocalizationData(true);
        }

        [Command("Autobrew-Abort", "Aborts the current brew", true, true, Platform.AllPlatforms, MonoTargetType.Single)]
        public static void Cmd_Abort()
        {
            BrewMaster.Abort(_abortCommand);
        }

        [Command("Autobrew-LogStatus", "Prints the status of the current order to the BepInEx console", true, true, Platform.AllPlatforms, MonoTargetType.Single)]
        public static void Cmd_LogStatus()
        {
            BrewMaster.LogCurrentStageProgress();
        }
    }
}