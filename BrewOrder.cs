using AutoBrew.PlotterConverter;
using BepInEx.Logging;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Salts;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew
{
    internal class BrewOrder
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        private static int _ingSaltBase;
        private static int _grindSaltBase;
        private static int _stirSaltBase;
        private static int _pourSaltBase;
        private static int _heatSaltBase;
        private static int _saltSaltBase;
        private static int _effectSaltBase;

        private static float _grindSaltMult;
        private static float _stirSaltMult;
        private static float _pourSaltMult;
        private static float _heatSaltMult;
        private static float _saltSaltMult;

        public static void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.SetOrigin("BrewOrder");
            ABSettings.GetInt(data, "SaltIngBase", out _ingSaltBase, 50);
            ABSettings.GetInt(data, "SaltGrindBase", out _grindSaltBase, 5);
            ABSettings.GetInt(data, "SaltStirBase", out _stirSaltBase, 5);
            ABSettings.GetInt(data, "SaltPourBase", out _pourSaltBase, 5);
            ABSettings.GetInt(data, "SaltHeatBase", out _heatSaltBase, 50);
            ABSettings.GetInt(data, "SaltSaltBase", out _saltSaltBase, 5);
            ABSettings.GetInt(data, "SaltEffectBase", out _effectSaltBase, 50);
            ABSettings.GetFloat(data, "SaltGrindMult", out _grindSaltMult, 10f);
            ABSettings.GetFloat(data, "SaltStirMult", out _stirSaltMult, 10f);
            ABSettings.GetFloat(data, "SaltPourMult", out _pourSaltMult, 10f);
            ABSettings.GetFloat(data, "SaltHeatMult", out _heatSaltMult, 10f);
            ABSettings.GetFloat(data, "SaltSaltMult", out _saltSaltMult, 0.1f);
        }

        public static BrewOrder IngOrderFromDict(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("item"))
            {
                Log.LogError("Order does not contain an ingredient to add");
                return null;
            }

            Ingredient ingItem = Ingredient.GetByName(data["item"]);
            if (ingItem == null)
            {
                Log.LogError($"Unknown ingredient '{data["item"]}' in order");
                return null;
            }

            float target = 0f;
            if (data.ContainsKey("target"))
            {
                if (float.TryParse(data["target"], out target))
                {
                    target = Mathf.Clamp(target, 0f, 100f) / 100f;
                }
            }
            return new BrewOrder(BrewStage.AddIngredient, target, item: ingItem);
        }

        public static BrewOrder GrindOrderFromDict(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("target"))
            {
                Log.LogError("You must specify a target for GrindPercent");
                return null;
            }
            if (!float.TryParse(data["target"], out float target))
            {
                Log.LogError($"GrindPercent needs a float, received '{data["target"]}'");
                return null;
            }
            if ((target < 0f) || (target > 100f))
            {
                Log.LogWarning($"GrindPercent must satisfy (0.0 <= x <= 100.0), received '{target}'");
                target = Mathf.Clamp(target, 0f, 100f);
            }
            
            //overallGrindStatus is a value between 0f and 1f
            target /= 100f;
            return new BrewOrder(BrewStage.GrindPercent, target);
        }
        
        public static BrewOrder StirOrderFromDict(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("target"))
            {
                Log.LogError("You must specify a target for StirCauldron");
                return null;
            }

            if (!float.TryParse(data["target"], out float target))
            {
                Log.LogError($"StirCauldron requires a float, received '{data["target"]}'");
                return null;
            }

            if (target < 0)
            {
                Log.LogError($"StirCauldron requires a float that is greater than or equal to 0, received '{data["target"]}'");
                return null;
            }
            return new BrewOrder(BrewStage.StirCauldron, target);
        }

        public static BrewOrder PourOrderFromDict(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("target"))
            {
                Log.LogError("You must specify a target for PourSolvent");
                return null;
            }

            if (!float.TryParse(data["target"], out float target))
            {
                Log.LogError($"PourSolvent requires a float, received '{data["target"]}'");
                return null;
            }

            if (target < 0)
            {
                Log.LogError($"PourSolvent requires a float that is greater than or equal to 0, received '{data["target"]}'");
                return null;
            }
            return new BrewOrder(BrewStage.PourSolvent, target);
        }

        public static BrewOrder HeatOrderFromDict(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("target"))
            {
                Log.LogError("You must specify a target for HeatVortex");
                return null;
            }

            if (!float.TryParse(data["target"], out float target))
            {
                Log.LogError($"HeatVortex requires a float, received '{data["target"]}'");
                return null;
            }

            if (!data.ContainsKey("version") || !int.TryParse(data["version"], out int version))
            {
                Log.LogWarning("No plotter version specified. Defaulting to an angle target");
                version = -1;
            }
            else if (!PlotterVortex.IsValidVersion(version))
            {
                Log.LogWarning($"Invalid plotter version given, received {version}");
                return null;
            }
            return new BrewOrder(BrewStage.HeatVortex, target, version);
        }

        public static BrewOrder SaltOrderFromDict(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("item"))
            {
                Log.LogError("You must specify a salt for AddSalt");
                return null;
            }

            if (!data.ContainsKey("target"))
            {
                Log.LogError("You must specify a target for AddSalt");
                return null;
            }

            Salt saltItem = Salt.GetByName(data["item"]);
            if (saltItem == null)
            {
                Log.LogError($"Unknown salt '{data["item"]}' in order");
                return null;
            }

            if (!int.TryParse(data["target"], out int amount))
            {
                Log.LogError($"SaltAmount needs an int, received '{data["target"]}'");
                return null;
            }

            if (amount < 1)
            {
                Log.LogError($"SaltAmount must be an int greater than or equal to 1. Received {amount}");
                return null;
            }
            return new BrewOrder(BrewStage.AddSalt, amount, item: saltItem);
        }

        public static BrewOrder EffectOrderFromDict(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("target"))
            {
                data["target"] = "0";
            }

            if (!int.TryParse(data["target"], out int target))
            {
                Log.LogError($"Method.FromJson: EffectOrder requires an int, received '{data["target"]}'");
                return null;
            }

            if ((target < 0) || (target > 3))
            {
                Log.LogError($"Method.FromJson - Order AddEffect requires an int that satisfies (0 <= x <= 3), received '{data["target"]}'");
                return null;
            }
            return new BrewOrder(BrewStage.AddEffect, target);
        }

        public static BrewOrder IngOrderFromPlotter(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("ingredientId"))
            {
                Log.LogError("Order does not contain an ingredient to add");
                return null;
            }

            Ingredient ingItem = Ingredient.GetByName(data["ingredientId"]);
            if (ingItem == null)
            {
                Log.LogError($"Unknown ingredient '{data["ingredientId"]}' in order");
                return null;
            }

            float target = 0f;
            if (data.ContainsKey("grindPercent"))
            {
                if (float.TryParse(data["grindPercent"], out target))
                {
                    target = Mathf.Clamp(target, 0f, 1f);
                }
            }
            return new BrewOrder(BrewStage.AddIngredient, target, item: ingItem);
        }

        public static BrewOrder GrindOrderFromPlotter(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("grindPercent"))
            {
                Log.LogError("You must specify a target for GrindPercent");
                return null;
            }
            if (!float.TryParse(data["grindPercent"], out float target))
            {
                Log.LogError($"GrindPercent needs a float, received '{data["grindPercent"]}'");
                return null;
            }
            if ((target < 0f) || (target > 1f))
            {
                Log.LogWarning($"GrindPercent must satisfy (0.0 <= x <= 1.0), received '{target}'");
                target = Mathf.Clamp(target, 0f, 1f);
            }
            return new BrewOrder(BrewStage.GrindPercent, target);
        }

        public static BrewOrder StirOrderFromPlotter(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("distance"))
            {
                Log.LogError("You must specify a target for StirCauldron");
                return null;
            }

            if (!float.TryParse(data["distance"], out float target))
            {
                Log.LogError($"StirCauldron requires a float, received '{data["distance"]}'");
                return null;
            }

            if (target < 0)
            {
                Log.LogError($"StirCauldron requires a float that is greater than or equal to 0, received '{data["distance"]}'");
                return null;
            }
            return new BrewOrder(BrewStage.StirCauldron, target);
        }

        public static BrewOrder PourOrderFromPlotter(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("distance"))
            {
                Log.LogError("You must specify a target for PourSolvent");
                return null;
            }

            if (!float.TryParse(data["distance"], out float target))
            {
                Log.LogError($"PourSolvent requires a float, received '{data["distance"]}'");
                return null;
            }

            if (target < 0)
            {
                Log.LogError($"PourSolvent requires a float that is greater than or equal to 0, received '{target}'");
                return null;
            }
            return new BrewOrder(BrewStage.PourSolvent, target);
        }

        public static BrewOrder HeatOrderFromPlotter(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("distance"))
            {
                Log.LogError("You must specify a target for HeatVortex");
                return null;
            }

            if (!float.TryParse(data["distance"], out float target))
            {
                Log.LogError($"HeatVortex requires a float, received '{data["distance"]}'");
                return null;
            }

            if (!data.ContainsKey("version") || !int.TryParse(data["version"], out int version))
            {
                Log.LogWarning("No plotter version specified. Defaulting to 0");
                version = 0;
            }
            else if (!PlotterVortex.IsValidVersion(version))
            {
                Log.LogWarning($"Invalid plotter version given, received {version}");
                return null;
            }
            return new BrewOrder(BrewStage.HeatVortex, target, version);
        }

        public static BrewOrder SaltOrderFromPlotter(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("salt"))
            {
                Log.LogError("You must specify a salt for AddSalt");
                return null;
            }

            if (!data.ContainsKey("grains"))
            {
                Log.LogError("You must specify a target for AddSalt");
                return null;
            }

            Salt saltItem = null;
            switch (data["salt"])
            {
                case "void":
                {
                    saltItem = Salt.GetByName("Void Salt");
                    break;
                }
                case "sun":
                {
                    saltItem = Salt.GetByName("Sun Salt");
                    break;
                }
                case "moon":
                {
                    saltItem = Salt.GetByName("Moon Salt");
                    break;
                }
            }
            
            if (saltItem == null)
            {
                Log.LogError($"Unknown salt '{data["salt"]}' in order");
                return null;
            }

            if (!int.TryParse(data["grains"], out int amount))
            {
                Log.LogError($"SaltAmount needs an int, received '{data["grains"]}'");
                return null;
            }

            if (amount < 1)
            {
                Log.LogError($"SaltAmount must be an int greater than or equal to 1. Received {amount}");
                return null;
            }
            return new BrewOrder(BrewStage.AddSalt, amount, item: saltItem);
        }

        public readonly BrewStage Stage;
        public readonly double Target;
        public readonly InventoryItem Item;
        public readonly int Version;

        public BrewOrder(BrewStage stage, double target = 0f, int version = -1, InventoryItem item = null)
        {
            Stage = stage;
            Target = target;
            Item = item;
            Version = version;
        }

        public int SaltCost => GetSaltCost();
        
        public int GetSaltCost()
        {
            switch (Stage)
            {
                case BrewStage.AddIngredient:
                {
                    return _ingSaltBase;
                }
                case BrewStage.GrindPercent:
                {
                    return _grindSaltBase + (int)Math.Ceiling(_grindSaltMult * Target);
                }
                case BrewStage.StirCauldron:
                {
                    return _stirSaltBase + (int)Math.Ceiling(_stirSaltMult * Target);
                }
                case BrewStage.PourSolvent:
                {
                    return _pourSaltBase + (int)Math.Ceiling(_pourSaltMult * Target);
                }
                case BrewStage.HeatVortex:
                {
                    return _heatSaltBase; // + (int)Math.Ceiling(_heatSaltMult * Target);
                }
                case BrewStage.AddSalt:
                {
                    return _saltSaltBase + (int)Math.Ceiling(_saltSaltMult * Target);
                }
                case BrewStage.AddEffect:
                {
                    return _effectSaltBase;
                }
            }
            return 100;
        }
    }
}
