using AutoBrew.PlotterConverter;
using BepInEx.Logging;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Salts;
using System.Collections.Generic;

namespace AutoBrew
{
    internal class BrewOrder
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

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
            return new BrewOrder(BrewStage.AddIngredient, item: ingItem);
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
            }
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
    }
}
