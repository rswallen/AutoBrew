using AutoBrew.Extensions;
using AutoBrew.Overseer;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PotionCraft.LocalizationSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ManagersSystem.Room;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.NotificationSystem;
using PotionCraft.ObjectBased.RecipeMap;
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
    internal class BrewMaster
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        private static readonly Key _brewStart = new("autobrew_brew_started");
        private static readonly Key _brewStartDesc = new("autobrew_brew_started_desc");
        private static readonly Key _brewComplete = new("autobrew_brew_complete");
        private static readonly Key _brewCompleteDesc = new("autobrew_brew_complete_desc");
        private static readonly Key _brewAbort = new("autobrew_brew_abort");
        private static readonly Key _brewAbortDef = new("autobrew_brew_abort_unknown");
        private static readonly Key _brewAbortNotInLab = new("autobrew_brew_abort_notinlab");
        private static readonly Key _brewAbortAdvFail = new("autobrew_brew_abort_advancefail");
        private static readonly Key _brewFalseStart = new("autobrew_brew_falsestart");
        private static readonly Key _brewFalseStartBrewing = new("autobrew_brew_falsestart_brewing");
        private static readonly Key _brewFalseStartJsonErr = new("autobrew_brew_falsestart_jsonerror");
        private static readonly Key _brewFalseStartUrlErr = new("autobrew_brew_falsestart_urlerror");
        private static readonly Key _brewFalseStartNotEnough = new("autobrew_brew_falsestart_notenough");
        private static readonly Key _brewFalseStartBaseIssue = new("autobrew_brew_falsestart_baseissue");

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

                _init = true;
            }
        }

        public static void Start()
        {
            LoadSettings();
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
                //Notification.ShowText("AutoBrew: Brew succeeded", "Brew complete", Notification.TextType.EventText);
                Notification.ShowText(_brewComplete.GetCustText(), _brewCompleteDesc.GetCustText(), Notification.TextType.EventText);
                Reset();
                return;
            }

            if (!VerifyInLab())
            {
                Abort(_brewAbortNotInLab);
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
            ABSettings.GetDouble(nameof(BrewMaster), data, "OrderInterval", out _orderInterval, 0.2, false);
            ABSettings.GetVector2(nameof(BrewMaster), data, "OrderCompleteOffset", out OrderCompleteOffset, new Vector2(0.5f, 2.5f), false);
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

        public static void Abort(Key reason)
        {
            if (_brewing)
            {
                reason ??= new("autobrew_brew_abort_unknown");
                Log.LogInfo($"Brew Aborted: {reason.GetDefText()}");
                Notification.ShowText(_brewAbort.GetCustText(), reason.GetCustText(), Notification.TextType.EventText);
                Reset();
            }
        }

        public static void InitBrew()
        {
            PotionCustomizationPanel customizer = Managers.Potion.potionCraftPanel.potionCustomizationPanel;
            if (PlotterUrlDecoder.IsPlotterURL(customizer.currentDescriptionText))
            {
                InitBrewFromPlotterURL();
            }
            else
            {
                InitBrewFromJson();
            }
        }

        public static void InitBrewFromJson()
        {
            if (Managers.SaveLoad.SystemState != SaveLoadManager.SystemStateEnum.Idle)
            {
                Log.LogInfo("Can't brew a potion during load or save");
                return;
            }

            if (Brewing)
            {
                Notification.ShowText(_brewFalseStart.GetCustText(), _brewFalseStartBrewing.GetCustText(), Notification.TextType.EventText);
                return;
            }

            if (!LoadJsonFromDesc())
            {
                Notification.ShowText(_brewFalseStart.GetCustText(), _brewFalseStartJsonErr.GetCustText(), Notification.TextType.EventText);
                return;
            }

            if (!CheckMapAndPotion())
            {
                Notification.ShowText(_brewFalseStart.GetCustText(), _brewFalseStartBaseIssue.GetCustText(), Notification.TextType.EventText);
                return;
            }

            if (CheckInventoryStock())
            {
                Notification.ShowText(_brewStart.GetCustText(), _brewStartDesc.GetCustText(), Notification.TextType.EventText);
            }
            else
            {
                Notification.ShowText(_brewFalseStart.GetCustText(), _brewFalseStartNotEnough.GetCustText(), Notification.TextType.EventText);
            }
        }

        public static void InitBrewFromPlotterURL()
        {
            if (Managers.SaveLoad.SystemState != SaveLoadManager.SystemStateEnum.Idle)
            {
                Log.LogInfo("Can't brew a potion during load or save");
                return;
            }

            if (Brewing)
            {
                Notification.ShowText(_brewFalseStart.GetCustText(), _brewFalseStartBrewing.GetCustText(), Notification.TextType.EventText);
                return;
            }

            if (!LoadPlotterUrlFromDesc())
            {
                Notification.ShowText(_brewFalseStart.GetCustText(), _brewFalseStartUrlErr.GetCustText(), Notification.TextType.EventText);
                return;
            }

            if (!CheckMapAndPotion())
            {
                Notification.ShowText(_brewFalseStart.GetCustText(), _brewFalseStartBaseIssue.GetCustText(), Notification.TextType.EventText);
                return;
            }

            if (CheckInventoryStock())
            {
                Notification.ShowText(_brewStart.GetCustText(), _brewStartDesc.GetCustText(), Notification.TextType.EventText);
            }
            else
            {
                Notification.ShowText(_brewFalseStart.GetCustText(), _brewFalseStartNotEnough.GetCustText(), Notification.TextType.EventText);
            }
        }

        public static bool LoadSettings()
        {
            JObject jsonData;
            Dictionary<string, string> newSettings;
            string basepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filepath = Path.Combine(basepath, "settings.json");
            
            if (!File.Exists(filepath))
            {
                Log.LogInfo("Settings file not detected. Using plugin defaults");
                var nullsettings = new Dictionary<string, string>();
                Reconfigure(nullsettings);
                BrewOrder.Reconfigure(nullsettings);
                BellowsOverseer.Reconfigure(nullsettings);
                CauldronOverseer.Reconfigure(nullsettings);
                InventoryOverseer.Reconfigure(nullsettings);
                MortarOverseer.Reconfigure(nullsettings);
                SolventOverseer.Reconfigure(nullsettings);
                return false;
            }

            try
            {
                jsonData = JObject.Parse(File.ReadAllText(filepath));

                newSettings = jsonData["BrewMaster"].ToObject<Dictionary<string, string>>();
                Reconfigure(newSettings);

                newSettings = jsonData["BrewOrder"].ToObject<Dictionary<string, string>>();
                BrewOrder.Reconfigure(newSettings);

                newSettings = jsonData["Bellows"].ToObject<Dictionary<string, string>>();
                BellowsOverseer.Reconfigure(newSettings);
                Boiler.Reset();

                newSettings = jsonData["Cauldron"].ToObject<Dictionary<string, string>>();
                CauldronOverseer.Reconfigure(newSettings);
                Stirrer.Reset();

                newSettings = jsonData["Inventory"].ToObject<Dictionary<string, string>>();
                InventoryOverseer.Reconfigure(newSettings);
                Larder.Reset();

                newSettings = jsonData["Mortar"].ToObject<Dictionary<string, string>>();
                MortarOverseer.Reconfigure(newSettings);
                Grinder.Reset();

                newSettings = jsonData["Solvent"].ToObject<Dictionary<string, string>>();
                SolventOverseer.Reconfigure(newSettings);
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

            PotionCustomizationPanel customizer = Managers.Potion.potionCraftPanel.potionCustomizationPanel;
            if (customizer.currentDescriptionText == string.Empty)
            {
                Log.LogError("Please paste json data into the custom description of the potion customizer panel");
                return false;
            }

            _recipe = BrewMethod.FromJson(customizer.currentDescriptionText);
            return ((_recipe != null) && (_recipe.Length != 0));
        }

        public static bool LoadPlotterUrlFromDesc()
        {
            if (Brewing)
            {
                return false;
            }

            PotionCustomizationPanel customizer = Managers.Potion.potionCraftPanel.potionCustomizationPanel;
            if (customizer.currentDescriptionText == string.Empty)
            {
                Log.LogError("Please paste plotter url into the custom description of the potion customizer panel");
                return false;
            }

            _recipe = BrewMethod.FromPlotterUrl(customizer.currentDescriptionText);
            return ((_recipe != null) && (_recipe.Length != 0));
        }

        public static bool CheckMapAndPotion()
        {
            if (_recipe.ResetAtStart)
            {
                Managers.Potion.ResetPotion();
            }

            // if base is not null, try to change the map
            if (_recipe.Base != null)
            {
                // if recipe base is the same as the base of the current map, lock it
                if (_recipe.Base.name.Equals(Managers.RecipeMap.currentMap.potionBase.name))
                {
                    MapLoader.MapChangeLock = true;
                    return true;
                }

                if (!Managers.RecipeMap.potionBaseSubManager.IsBaseUnlocked(_recipe.Base))
                {
                    // if base is not unlocked, fail
                    return false;
                }

                if (MapLoader.MapChangeLock || Managers.Potion.potionCraftPanel.IsPotionBrewingStarted())
                {
                    // if map lock is active, or if (manual) brewing was already started, fail
                    // (can't change map if either are true)
                    return false;
                }

                // if all other checks have passed, change the map and lock it
                PotionOverseer.SelectMapAndLock(_recipe.Base, true);
            }
            return true;
        }

        public static bool CheckInventoryStock()
        {
            var items = _recipe.GetItemsRequired();
            if (!Larder.CheckItemStock(ref items))
            {
                // not enough of something or typo
                foreach ((InventoryItem item, int count) in items.Select(kvp => (kvp.Key, kvp.Value)))
                {
                    if (count == 0)
                    {
                        Log.LogDebug($"Not enough '{item.name}' to brew");
                    }
                }
                Log.LogDebug("Error detected in ingredients. Cancelling brew.");
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
                    Abort(_brewAbortAdvFail);
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

        public static bool VerifyInLab()
        {
            return Managers.Room.currentRoom == RoomManager.RoomIndex.Laboratory && !Managers.Room.CameraMover.IsMoving();
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
            Log.LogError(order);
        }

        public static void PrintRecipeMapMessage(string message, Vector2 offset)
        {
            RecipeMapObject recipeMapObject = Managers.RecipeMap.recipeMapObject;

            //var prefab = Settings<RecipeMapManagerPotionBasesSettings>.Asset.floatingTextSelectBase;
            var prefab = Settings<PotionManagerSettings>.Asset.collectedFloatingTextPrefab;
            
            Vector2 msgPos = recipeMapObject.transmitterWindow.ViewRect.center + offset;
            CollectedFloatingText.SpawnNewText(prefab, msgPos, new CollectedFloatingText.FloatingTextContent(message, CollectedFloatingText.FloatingTextContent.Type.Text, 0f), Managers.Game.Cam.transform, false, false);
        }

        public static void PrintRecipeMapMessage(Key message, Vector2 offset)
        {
            RecipeMapObject recipeMapObject = Managers.RecipeMap.recipeMapObject;
            var prefab = Settings<PotionManagerSettings>.Asset.collectedFloatingTextPrefab;
            Vector2 msgPos = recipeMapObject.transmitterWindow.ViewRect.center + offset;
            CollectedFloatingText.SpawnNewText(prefab, msgPos, new CollectedFloatingText.FloatingTextContent(message.GetCustText(), CollectedFloatingText.FloatingTextContent.Type.Text, 0f), Managers.Game.Cam.transform, false, false);
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
