using AutoBrew.Extensions;
using AutoBrew.JsonObjects;
using BepInEx.Logging;
using Newtonsoft.Json;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Salts;
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

            BrewMethod method = new();
            method.ProcessJsonOrders(buffer);
            return method;
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

            PotionBase potionbase = null;
            if (!string.IsNullOrEmpty(buffer.PotionBaseId))
            {
                potionbase = PotionBase.GetByName(buffer.PotionBaseId, false, false);
            }
            
            BrewMethod method = new(potionbase, true);
            method.ProcessJsonOrders(buffer2);
            return method;
        }

        private readonly List<BrewOrder> _data;
        private int currentIndex;

        public readonly PotionBase Base;
        public readonly bool ResetAtStart;

        public bool Complete { get { return currentIndex >= _data.Count; } }
        public int Length { get { return _data.Count; } }

        public BrewMethod(PotionBase potionbase = null, bool reset = false)
        {
            Base = potionbase;
            ResetAtStart = reset;
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

        public void ProcessJsonOrders(List<JsonOrder> data)
        {
            foreach (var order in data)
            {
                switch (order.Order)
                {
                    case BrewOrderType.AddIngredient:
                    {
                        var ingOrder = order.GetBrewOrder();
                        if (ingOrder == null)
                        {
                            Log.LogError($"Error detected in order '{order}'");
                            break;
                        }
                        AddOrder(ingOrder);
                        if (!ingOrder.Target.Is(0.0))
                        {
                            AddOrder(order.GetBrewOrder(BrewOrderType.GrindPercent));
                        }
                        break;
                    }
                    case BrewOrderType.StirCauldron:
                    case BrewOrderType.PourSolvent:
                    case BrewOrderType.HeatVortex:
                    case BrewOrderType.AddSalt:
                    case BrewOrderType.AddEffect:
                    {
                        var bOrder = order.GetBrewOrder();
                        if (bOrder == null)
                        {
                            Log.LogError($"Error detected in order '{order}'");
                            break;
                        }
                        AddOrder(bOrder);
                        break;
                    }
                }
            }
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
                    case BrewOrderType.AddIngredient:
                    case BrewOrderType.AddSalt:
                    {
                        items.TryGetValue(order.Item, out int current);
                        items[order.Item] = current + 1;
                        break;
                    }
                    default: break;
                }
                saltCost += order.SaltCost;
            }

            Salt saltType = Salt.GetByName("Void Salt", false, false);
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
