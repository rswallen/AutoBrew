using AutoBrew.Extensions;
using AutoBrew.PlotterConverter;
using Newtonsoft.Json;
using PotionCraft.ScriptableObjects;
using System;

namespace AutoBrew.JsonObjects
{
    internal class JsonOrder
    {
        public BrewStage Order = BrewStage.Idle;
        public InventoryItem InvItem = null;

        [JsonProperty("order")]
        public string Stage
        {
            get { return Order.ToString(); }
            set { Enum.TryParse(value, out Order); }
        }

        [JsonProperty("version")]
        public int Version = -1;

        [JsonProperty("item")]
        public string Item
        {
            get { return InvItem?.name ?? ""; }
            set { InvItem = string.IsNullOrEmpty(value) ? null : InventoryItem.GetByName(value); }
        }

        [JsonProperty("target")]
        public double Target = 0.0;

        public BrewOrder GetBrewOrder()
        {
            return GetBrewOrder(Order);
        }

        public BrewOrder GetBrewOrder(BrewStage type)
        {
            if (Target < 0.0) return null;

            switch (type)
            {
                case BrewStage.AddIngredient:
                case BrewStage.GrindPercent:
                {
                    return new BrewOrder(type, Target.Clamp01(), Version, InvItem);
                }
                case BrewStage.AddSalt:
                {
                    return (Target < 1.0) ? null : new BrewOrder(type, Target, Version, InvItem);
                }
                case BrewStage.HeatVortex:
                {
                    return !PlotterVortex.IsValidVersion(Version) ? null : new BrewOrder(type, Target, Version, null);
                }
                case BrewStage.StirCauldron:
                case BrewStage.PourSolvent:
                case BrewStage.AddEffect:
                {
                    return new BrewOrder(type, Target, Version, null);
                }
                default:
                {
                    return null;
                }
            }
        }

        public override string ToString()
        {
            return $"{{ Order: '{Stage,15}', Version: '{Version,2}', Item: '{Item,15}', Target: '{Target}' }}";
        }
    }
}
