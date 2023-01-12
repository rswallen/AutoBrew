﻿using AutoBrew.Extensions;
using AutoBrew.Overseer;
using AutoBrew.UIElements;
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
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraft.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.UIElements.StylePropertyAnimationSystem;

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
        private static BrewState state = BrewState.Idle;
        private static BrewMode mode = BrewMode.Continuous;
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

        public static readonly UnityEvent<BrewState> OnStateChanged = new();
        public static readonly UnityEvent<BrewMode> OnModeChanged = new();
        public static readonly UnityEvent<int, BrewOrder> OnOrderStarted = new();
        public static readonly UnityEvent<int, BrewOrder> OnOrderCompleted = new();
        public static readonly UnityEvent<int, BrewOrder> OnOrderFailed = new();

        public static bool Initialised
        {
            get { return _init; }
        }

        public static bool Brewing
        {
            get { return state != BrewState.Idle; }
        }

        public static BrewState State
        {
            get { return state; }
            set { ChangeBrewState(value); }
        }

        public static BrewMode Mode
        {
            get { return mode; }
            set { ChangeBrewMode(value); }
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
            switch (State)
            {
                case BrewState.Complete:
                {
                    Log.LogInfo("Brew succeeded");
                    //Notification.ShowText("AutoBrew: Brew succeeded", "Brew complete", Notification.TextType.EventText);
                    Notification.ShowText(_brewComplete.GetAutoBrewText(), _brewCompleteDesc.GetAutoBrewText(), Notification.TextType.EventText);
                    Reset();
                    return;
                }
                case BrewState.Brewing:
                {
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
                    return;
                }
                default:
                {
                    return;
                }
            }
        }

        public static void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetDouble(nameof(BrewMaster), data, "OrderInterval", out _orderInterval, 0.2, false);
            ABSettings.GetVector2(nameof(BrewMaster), data, "OrderCompleteOffset", out OrderCompleteOffset, new Vector2(0.5f, 2.5f), false);
        }

        public static void Reset()
        {
            State = BrewState.Idle;
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
            if (Brewing)
            {
                reason ??= new("autobrew_brew_abort_unknown");
                Log.LogInfo($"Brew Aborted: {reason.GetAutoBrewText(LocalizationManager.Locale.en)}");
                Notification.ShowText(_brewAbort.GetAutoBrewText(), reason.GetAutoBrewText(), Notification.TextType.EventText);
                Reset();
            }
        }

        public static void InitBrew()
        {
            string data = Managers.Potion.potionCraftPanel.potionCustomizationPanel.currentDescriptionText;
            ParseRecipe(data);
        }

        public static void ParseRecipe(string data)
        {
            if (Managers.SaveLoad.SystemState != SaveLoadManager.SystemStateEnum.Idle)
            {
                Log.LogInfo("Can't brew a potion during load or save");
                return;
            }

            if (Brewing)
            {
                Notification.ShowText(_brewFalseStart.GetAutoBrewText(), _brewFalseStartBrewing.GetAutoBrewText(), Notification.TextType.EventText);
                return;
            }

            //PotionCustomizationPanel customizer = Managers.Potion.potionCraftPanel.potionCustomizationPanel;
            //if (PlotterUrlDecoder.IsPlotterURL(customizer.currentDescriptionText))
            if (PlotterUrlDecoder.IsPlotterURL(data))
            {
                if (!LoadPlotterUrl(data))
                {
                    Notification.ShowText(_brewFalseStart.GetAutoBrewText(), _brewFalseStartUrlErr.GetAutoBrewText(), Notification.TextType.EventText);
                    return;
                }
            }
            else
            {
                if (!LoadJson(data))
                {
                    Notification.ShowText(_brewFalseStart.GetAutoBrewText(), _brewFalseStartJsonErr.GetAutoBrewText(), Notification.TextType.EventText);
                    return;
                }
            }

            UIManager.Cookbook.LoadMethod(_recipe);
        }

        public static void StartBrew()
        {
            if (!CheckMapAndPotion())
            {
                Notification.ShowText(_brewFalseStart.GetAutoBrewText(), _brewFalseStartBaseIssue.GetAutoBrewText(), Notification.TextType.EventText);
                return;
            }

            if (CheckInventoryStock())
            {
                Notification.ShowText(_brewStart.GetAutoBrewText(), _brewStartDesc.GetAutoBrewText(), Notification.TextType.EventText);
            }
            else
            {
                Notification.ShowText(_brewFalseStart.GetAutoBrewText(), _brewFalseStartNotEnough.GetAutoBrewText(), Notification.TextType.EventText);
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

        public static bool LoadJson(string data)
        {
            if (Brewing)
            {
                return false;
            }

            if (string.IsNullOrEmpty(data))
            {
                Log.LogError("Please paste json data into the custom description of the potion customizer panel");
                return false;
            }

            _recipe = BrewMethod.FromJson(data);
            return ((_recipe != null) && (_recipe.Length != 0));
        }

        public static bool LoadPlotterUrl(string data)
        {
            if (Brewing)
            {
                return false;
            }

            if (string.IsNullOrEmpty(data))
            {
                Log.LogError("Please paste a plotter url into the custom description of the potion customizer panel");
                return false;
            }

            _recipe = BrewMethod.FromPlotterUrl(data);
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
                    MapStatesManager.MapChangeLock = true;
                    return true;
                }

                if (!Managers.RecipeMap.potionBaseSubManager.IsBaseUnlocked(_recipe.Base))
                {
                    // if base is not unlocked, fail
                    return false;
                }

                if (MapStatesManager.MapChangeLock || Managers.Potion.potionCraftPanel.IsPotionBrewingStarted())
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
            if (!InventoryOverseer.CheckItemStock(ref items))
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

            State = BrewState.Brewing;
            OnOrderStarted.Invoke(_recipe.CurrentVisibleIndex, null);
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

                if (_recipe.Complete)
                {
                    State = BrewState.Complete;
                    return;
                }

                if (Mode == BrewMode.StepForward)
                {
                    State = BrewState.Paused;
                }

                if (_recipe.GetCurrentOrder(out BrewOrder order))
                {
                    OnOrderStarted.Invoke(_recipe.CurrentVisibleIndex, order);
                }
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
                case BrewOrderType.AddIngredient:
                case BrewOrderType.AddSalt:
                {
                    return Larder;
                }
                case BrewOrderType.GrindPercent:
                {
                    return Grinder;
                }
                case BrewOrderType.StirCauldron:
                {
                    return Stirrer;
                }
                case BrewOrderType.PourSolvent:
                {
                    return Pourer;
                }
                case BrewOrderType.HeatVortex:
                case BrewOrderType.AddEffect:
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
            CollectedFloatingText.SpawnNewText(prefab, msgPos, new CollectedFloatingText.FloatingTextContent(message.GetAutoBrewText(), CollectedFloatingText.FloatingTextContent.Type.Text, 0f), Managers.Game.Cam.transform, false, false);
        }

        private static void ChangeBrewState(BrewState newValue)
        {
            if (state != newValue)
            {
                state = newValue;
                OnStateChanged.Invoke(newValue);
            }
        }

        private static void ChangeBrewMode(BrewMode newValue)
        {
            if (mode != newValue)
            {
                mode = newValue;
                OnModeChanged.Invoke(newValue);
            }
        }
    }

    public enum BrewMode
    {
        Continuous,
        StepForward,
    }

    public enum BrewState
    {
        Idle,
        Brewing,
        Paused,
        Complete,
        Aborted,
    }

    public enum BrewOrderType
    {
        Idle,
        AddIngredient,
        GrindPercent,
        StirCauldron,
        PourSolvent,
        HeatVortex,
        AddSalt,
        AddEffect,
        Complete,
    }
}
