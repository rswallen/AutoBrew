using AutoBrew.JsonObjects;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Salts;
using System;
using System.Linq;
using System.Collections.Generic;

namespace AutoBrew
{
    internal sealed class BrewMethod
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        public static BrewMethod FromJson(string json)
        {
            List<JsonOrder> buffer;
            try
            {
                buffer = JsonConvert.DeserializeObject<List<JsonOrder>>(json);
                Log.LogInfo("Deserialised JSON successfully");
            }
            catch (JsonReaderException e)
            {
                Log.LogError("Json parse error: " + e.ToString());
                return null;
            }

            if (buffer == null)
            {
                Log.LogError("Deserialised to null");
                return null;
            }
            return ProcessJsonOrders(buffer);
        }
        
        public static BrewMethod FromPlotterUrl(string url)
        {
            string json = PlotterUrlDecoder.ProcessURL(url);
            if (json == null)
            {
                return null;
            }

            var buffer = JsonConvert.DeserializeObject<PlotterRecipe>(json);
            if ((buffer == null) || (buffer.PlotItems == null))
            {
                Log.LogError("Deserialised to null");
                return null;
            }
            var buffer2 = buffer.PlotItems.Cast<JsonOrder>().ToList();
            return ProcessJsonOrders(buffer2);
        }

        public static BrewMethod ProcessJsonOrders(List<JsonOrder> data)
        {
            BrewMethod method = new();
            foreach (var order in data)
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
            }
            return method;
        }

        private readonly List<BrewOrder> _data;
        private int currentIndex;

        public bool Complete { get { return currentIndex >= _data.Count; } }
        public int Length { get { return _data.Count; } }

        public BrewMethod()
        {
            _data = new();
        }

        public List<BrewOrder> OrderList
        {
            get { return _data; }
        }

        public void AddOrder(BrewOrder order)
        {
            if (order == null)
            {
                Log.LogError($"BrewMethod: Cannot add null order");
                return;
            }
            _data.Add(order);
            Log.LogDebug($"Method: Parsed order '{order}'");
        }

        public bool GetOrder(int idx, out BrewOrder order)
        {
            if ((idx < 0) || (idx >= _data.Count))
            {
                order = null;
                return false;
            }

            order = _data[idx];
            return true;
        }

        public bool GetCurrentOrder(out BrewOrder order)
        {
            return GetOrder(currentIndex, out order);
        }

        public bool Advance()
        {
            int orders = _data.Count;
            if (currentIndex >= orders)
            {
                return false;
            }
            currentIndex++;
            return true;
        }

        public Dictionary<InventoryItem, int> GetItemsRequired()
        {
            int saltCost = 0;
            Dictionary<InventoryItem, int> items = new();
            foreach (BrewOrder order in _data)
            {
                switch (order.Stage)
                {
                    case BrewStage.AddIngredient:
                    case BrewStage.AddSalt:
                    {
                        items.TryGetValue(order.Item, out int current);
                        items[order.Item] = current + 1;
                        break;
                    }
                    default: break;
                }
                saltCost += order.SaltCost;
            }

            Salt saltType = Salt.GetByName("Void Salt", true, true);
            if (saltType == null)
            {
                Log.LogError("Could not find salt 'Void Salt', so this one is free");
            }
            else
            {
                items.TryGetValue(saltType, out int current);
                items[saltType] = current + saltCost;
            }
            return items;
        }

        public int GetSaltCost()
        {
            int cost = 0;
            foreach (BrewOrder order in _data)
            {
                cost += order.SaltCost;
            }
            return cost;
        }
    }
}
