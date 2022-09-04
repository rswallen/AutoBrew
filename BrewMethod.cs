using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Salts;
using System;
using System.Collections.Generic;

namespace AutoBrew
{
    internal sealed class BrewMethod
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        public static BrewMethod FromJson(string json)
        {
            List<Dictionary<string, string>> buffer;
            try
            {
                buffer = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
                Log.LogInfo("Deserialised JSON successfully");
            }
            catch (JsonReaderException e)
            {
                Log.LogError("Json parse error: " + e.ToString());
                return null;
            }

            BrewMethod _method = new();
            foreach (var dict in buffer)
            {
                _method.AddOrder(dict);
            }
            return _method;
        }

        public static BrewMethod FromPlotterUrl(string url)
        {
            string json = PlotterUrlDecoder.ProcessURL(url);
            if (json == null)
            {
                return null;
            }

            var jobj = JObject.Parse(json);
            if (jobj == null)
            {
                return null;
            }

            var id = jobj.GetValue("datasetId");
            if (id == null || id.ToString() != "2XP7bCigUeobr2HDtbWUrF")
            {
                return null;
            }

            var plotItems = jobj.GetValue("plotItems");
            if (plotItems == null)
            {
                return null;
            }

            var buffer = new List<Dictionary<string, string>>();
            foreach (var item in (JArray)plotItems)
            {
                if (item is not JObject dict)
                {
                    break;
                }
                buffer.Add(dict.ToObject<Dictionary<string, string>>());
            }

            BrewMethod _method = new();
            foreach (var dict in buffer)
            {
                _method.AddPlotterOrder(dict);
            }
            return _method;
        }

        private readonly List<BrewOrder> _data;
        private int currentIndex;

        public bool Complete { get { return currentIndex >= _data.Count; } }
        public int Length { get { return _data.Count; } }

        public BrewMethod()
        {
            _data = new();
        }

        public bool AddOrder(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("order"))
            {
                return false;
            }

            if (!Enum.TryParse(data["order"], out BrewStage order))
            {
                Log.LogError($"Method.FromJson - Unknown order type: {data["order"]}");
                return false;
            }

            switch (order)
            {
                case BrewStage.AddIngredient:
                {
                    var ingOrder = BrewOrder.IngOrderFromDict(data);
                    if (ingOrder == null)
                    {
                        return false;
                    }

                    if (ingOrder.Target != 0f)
                    {
                        var grindOrder = BrewOrder.GrindOrderFromDict(data);
                        if (grindOrder != null)
                        {
                            this.AddOrder(ingOrder);
                            this.AddOrder(grindOrder);
                            return true;
                        }
                        return false;
                    }

                    this.AddOrder(ingOrder);
                    return true;
                }
                case BrewStage.PourSolvent:
                {
                    var pourOrder = BrewOrder.PourOrderFromDict(data);
                    if (pourOrder != null)
                    {
                        this.AddOrder(pourOrder);
                    }
                    break;
                }
                case BrewStage.StirCauldron:
                {
                    var stirOrder = BrewOrder.StirOrderFromDict(data);
                    if (stirOrder != null)
                    {
                        this.AddOrder(stirOrder);
                        return true;
                    }
                    break;
                }
                case BrewStage.HeatVortex:
                {
                    var heatOrder = BrewOrder.HeatOrderFromDict(data);
                    if (heatOrder != null)
                    {
                        this.AddOrder(heatOrder);
                        return true;
                    }
                    break;
                }
                case BrewStage.AddSalt:
                {
                    var saltOrder = BrewOrder.SaltOrderFromDict(data);
                    if (saltOrder != null)
                    {
                        this.AddOrder(saltOrder);
                        return true;
                    }
                    break;
                }
                case BrewStage.AddEffect:
                {
                    var effectOrder = BrewOrder.EffectOrderFromDict(data);
                    if (effectOrder != null)
                    {
                        this.AddOrder(effectOrder);
                        return true;
                    }
                    break;
                }
            }

            return false;
        }

        public bool AddPlotterOrder(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("type"))
            {
                return false;
            }

            switch (data["type"])
            {
                case "add-ingredient":
                {
                    var ingOrder = BrewOrder.IngOrderFromPlotter(data);
                    if (ingOrder == null)
                    {
                        return false;
                    }

                    if (ingOrder.Target != 0f)
                    {
                        var grindOrder = BrewOrder.GrindOrderFromPlotter(data);
                        if (grindOrder != null)
                        {
                            this.AddOrder(ingOrder);
                            this.AddOrder(grindOrder);
                            return true;
                        }
                        return false;
                    }

                    this.AddOrder(ingOrder);
                    return true;
                }
                case "pour-solvent":
                {
                    var pourOrder = BrewOrder.PourOrderFromPlotter(data);
                    if (pourOrder != null)
                    {
                        this.AddOrder(pourOrder);
                    }
                    break;
                }
                case "stir-cauldron":
                {
                    var stirOrder = BrewOrder.StirOrderFromPlotter(data);
                    if (stirOrder != null)
                    {
                        this.AddOrder(stirOrder);
                        return true;
                    }
                    break;
                }
                case "heat-vortex":
                {
                    var heatOrder = BrewOrder.HeatOrderFromPlotter(data);
                    if (heatOrder != null)
                    {
                        this.AddOrder(heatOrder);
                        return true;
                    }
                    break;
                }
                case "add-rotation-salt":
                {
                    data["type"] = "add-salt";
                    var saltOrder = BrewOrder.SaltOrderFromPlotter(data);
                    if (saltOrder != null)
                    {
                        this.AddOrder(saltOrder);
                        return true;
                    }
                    break;
                }
                case "void-salt":
                {
                    data["type"] = "add-salt";
                    data["salt"] = "void";
                    var saltOrder = BrewOrder.SaltOrderFromPlotter(data);
                    if (saltOrder != null)
                    {
                        this.AddOrder(saltOrder);
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        public void AddOrder(BrewOrder order)
        {
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
