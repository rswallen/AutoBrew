using BepInEx.Logging;
using Newtonsoft.Json;
using PotionCraft.Npc.Browser;
using PotionCraft.Npc.Parts;
using System;
using System.Collections.Generic;

namespace AutoBrew.JsonObjects
{
    internal class PlotterRecipe
    {
        static ManualLogSource Log => AutoBrewPlugin.Log;

        [JsonProperty("datasetId")]
        public string DatasetId { get; set; }

        [JsonProperty("potionBaseId")]
        public string PotionBaseId { get; set; }

        [JsonProperty("plotItems")]
        public List<PlotterOrder> PlotItems { get; set; }

        public BrewMethod GetBrewMethod()
        {
            BrewMethod method = new();
            foreach (var order in PlotItems)
            {
                switch (order.Order)
                {
                    case BrewStage.AddIngredient:
                        {
                            var ingOrder = order.GetBrewOrder();
                            if (ingOrder == null)
                            {
                                Log.LogError($"Error detected in order '{order}'");
                                break;
                            }
                            method.AddOrder(ingOrder);
                            method.AddOrder(order.GetBrewOrder(BrewStage.GrindPercent));
                            break;
                        }
                    case BrewStage.StirCauldron:
                    case BrewStage.PourSolvent:
                    case BrewStage.HeatVortex:
                    case BrewStage.AddSalt:
                    case BrewStage.AddEffect:
                        {
                            var bOrder = order.GetBrewOrder();
                            if (bOrder == null)
                            {
                                Log.LogError($"Error detected in order '{order}'");
                                break;
                            }
                            method.AddOrder(bOrder);
                            break;
                        }
                }
                method.AddOrder(order.GetBrewOrder());
            }
            return method;
        }
    }
}