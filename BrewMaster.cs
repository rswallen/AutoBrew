using AutoBrew.Overseer;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.NotificationSystem;
using PotionCraft.ObjectBased.RecipeMap.RecipeMapObject;
using PotionCraft.ObjectBased.UIElements.FloatingText;
using PotionCraft.ObjectBased.UIElements.PotionCustomizationPanel;
using PotionCraft.ScriptableObjects;
using PotionCraft.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AutoBrew
{
    internal static class BrewMaster
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        private static bool _init;
        private static bool _brewing;
        private static double _orderInterval;
        public static Vector2 OrderCompleteOffset;
        private static double _gtLastOrder;
        private static BrewMethod _recipe;

        public static DebugOverseer Debug { get; private set; }
        public static InventoryOverseer Larder { get; private set; }
        public static MortarOverseer Grinder { get; private set; }
        public static CauldronOverseer Stirrer { get; private set; }
        public static SolventOverseer Pourer { get; private set; }
        public static BellowsOverseer Boiler { get; private set; }

        public static bool Initialised
        {
            get { return _init; }
        }

        public static bool Brewing
        {
            get { return _brewing; }
        }

        public static void Awake()
        {
            if (!_init)
            {
                Debug ??= new DebugOverseer();
                Larder ??= new InventoryOverseer();
                Grinder ??= new MortarOverseer();
                Stirrer ??= new CauldronOverseer();
                Pourer ??= new SolventOverseer();
                Boiler ??= new BellowsOverseer();
                Reset();

                LoadSettings();
                _init = true;
            }
        }

        public static void Update()
        {
            if (!Brewing)
            {
                return;
            }

            if (_recipe.Complete)
            {
                Log.LogInfo("Brew succeeded");
                Notification.ShowText("AutoBrew: Brew succeeded", "Brew complete", Notification.TextType.EventText);
                Reset();
                return;
            }

            double gtNextOrder = _gtLastOrder + _orderInterval;
            if (gtNextOrder > Time.timeAsDouble)
            {
                return;
            }

            _recipe.GetCurrentOrder(out BrewOrder order);
            GetStageOverseer(order).Update(order);
        }

        public static void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.SetOrigin("BrewMaster");
            ABSettings.GetDouble(data, "OrderInterval", out _orderInterval, 0.2, false);
            ABSettings.GetVector2(data, "OrderCompleteOffset", out OrderCompleteOffset, new Vector2(0.5f, 2.5f), false);
        }

        public static void Reset()
        {
            _brewing = false;
            _recipe = null;
            _gtLastOrder = Time.timeAsDouble;

            Debug.Reset();
            Larder.Reset();
            Grinder.Reset();
            Stirrer.Reset();
            Pourer.Reset();
            Boiler.Reset();
        }

        public static void Abort(string reason)
        {
            if (_brewing)
            {
                if (reason == string.Empty)
                {
                    reason = "Something went wrong";
                }
                Log.LogInfo($"Brew Aborted: {reason}");
                Notification.ShowText("AutoBrew: Brew aborted", reason, Notification.TextType.EventText);
                Reset();
            }
        }

        public static void InitBrew()
        {
            if (LoadJsonFromDesc())
            {
                if (GetBrewing())
                {
                    Notification.ShowText("AutoBrew: Brew started", "And so it begins", Notification.TextType.EventText);
                }
            }
        }

        public static bool LoadSettings()
        {
            JObject jsonData;
            Dictionary<string, string> newSettings;
            string filepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/settings.json";
            if (!File.Exists(filepath))
            {
                Log.LogInfo("Settings file not detected. Using plugin defaults");
                return false;
            }

            try
            {
                jsonData = JObject.Parse(File.ReadAllText(filepath));

                newSettings = jsonData["BrewMaster"].ToObject<Dictionary<string, string>>();
                Reconfigure(newSettings);

                newSettings = jsonData["Bellows"].ToObject<Dictionary<string, string>>();
                Boiler.Reconfigure(newSettings);
                Boiler.Reset();

                newSettings = jsonData["Cauldron"].ToObject<Dictionary<string, string>>();
                Stirrer.Reconfigure(newSettings);
                Stirrer.Reset();

                newSettings = jsonData["Inventory"].ToObject<Dictionary<string, string>>();
                Larder.Reconfigure(newSettings);
                Larder.Reset();

                newSettings = jsonData["Mortar"].ToObject<Dictionary<string, string>>();
                Grinder.Reconfigure(newSettings);
                Grinder.Reset();

                newSettings = jsonData["Solvent"].ToObject<Dictionary<string, string>>();
                Pourer.Reconfigure(newSettings);
                Pourer.Reset();

            }
            catch (JsonReaderException e)
            {
                Log.LogError("Error parsing settings: ");
                Log.LogError(e.ToString());
            }
            catch (Exception e)
            {
                Log.LogError("Unknown error: ");
                Log.LogError(e.ToString());
            }

            return true;
        }

        public static bool LoadJsonFromDesc()
        {
            if (Brewing)
            {
                return false;
            }

            if (Managers.SaveLoad.SystemState != SaveLoadManager.SystemStateEnum.Idle)
            {
                Log.LogInfo("Can't brew a potion during load or save");
                return false;
            }

            PotionCustomizationPanel customizer = Managers.Potion.potionCraftPanel.potionCustomizationPanel;

            string data = string.Copy(customizer.currentDescriptionText);
            if (data == string.Empty)
            {
                Log.LogInfo("Please paste json data into the custom description of the potion customizer panel");
                return false;
            }

            _recipe = BrewMethod.FromJson(data);
            return ((_recipe != null) && (_recipe.Length != 0));
        }

        public static bool GetBrewing()
        {
            var items = _recipe.GetItemsRequired();
            if (!Larder.CheckItemStock(ref items))
            {
                // not enough of something or typo
                foreach ((InventoryItem item, int count) in items.Select(kvp => (kvp.Key, kvp.Value)))
                {
                    if (count == 0)
                    {
                        Log.LogInfo($"Not enough '{item.name}' to brew");
                    }
                }
                Log.LogInfo("Error detected in ingredients. Cancelling brew.");
                return false;
            }
            Log.LogInfo("We have enough ingredients. Proceeding to brew.");

            _brewing = true;
            return true;
        }

        public static void AdvanceOrder()
        {
            if (_recipe != null)
            {
                if (!_recipe.Advance())
                {
                    Abort("Could not advance the BrewMethod");
                }
                _gtLastOrder = Time.timeAsDouble;
            }
        }

        public static void GetCurrentInstruction(out BrewOrder order)
        {
            _recipe.GetCurrentOrder(out order);
        }

        public static BaseOverseer GetStageOverseer(BrewOrder order)
        {
            switch (order.Stage)
            {
                case BrewStage.AddIngredient:
                case BrewStage.AddSalt:
                {
                    return Larder;
                }
                case BrewStage.GrindPercent:
                {
                    return Grinder;
                }
                case BrewStage.StirCauldron:
                {
                    return Stirrer;
                }
                case BrewStage.PourSolvent:
                {
                    return Pourer;
                }
                case BrewStage.HeatVortex:
                case BrewStage.AddEffect:
                {
                    return Boiler;
                }
            }
            return Debug;
        }

        public static void LogCurrentStageProgress()
        {
            if (!Brewing)
            {
                Log.LogInfo("Brewer idle");
                return;
            }

            _recipe.GetCurrentOrder(out BrewOrder order);
            GetStageOverseer(order).LogStatus();
        }

        public static void LogFailedOrder(BrewOrder order)
        {
            Log.LogError($"Brew Cancelled - {order.Stage} failed");
        }

        public static void PrintRecipeMapMessage(string message, Vector2 offset)
        {
            RecipeMapObject recipeMapObject = Managers.RecipeMap.recipeMapObject;

            //var prefab = Settings<RecipeMapManagerPotionBasesSettings>.Asset.floatingTextSelectBase;
            var prefab = Settings<PotionManagerSettings>.Asset.collectedFloatingTextPrefab;
            
            Vector2 msgPos = recipeMapObject.transmitterWindow.ViewRect.center + offset;
            CollectedFloatingText.SpawnNewText(prefab, msgPos, new CollectedFloatingText.FloatingTextContent(message, CollectedFloatingText.FloatingTextContent.Type.Text, 0f), Managers.Game.Cam.transform, false, false);
        }
    }

    public enum BrewStage
    {
        Idle,
        AddIngredient,
        GrindPercent,
        StirCauldron,
        PourSolvent,
        HeatVortex,
        AddSalt,
        AddEffect,
        Complete
    }
}
