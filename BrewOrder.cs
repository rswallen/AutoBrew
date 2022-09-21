using PotionCraft.LocalizationSystem;
using PotionCraft.ScriptableObjects;
using System;
using System.Collections.Generic;

namespace AutoBrew
{
    internal class BrewOrder
    {
        private static readonly Key _brewOrderFailAddIng = new("autobrew_orderfail_addingredient");
        private static readonly Key _brewOrderFailGrind = new("autobrew_orderfail_grindpercent");
        private static readonly Key _brewOrderFailStir = new("autobrew_orderfail_stircauldron");
        private static readonly Key _brewOrderFailPour = new("autobrew_orderfail_poursolvent");
        private static readonly Key _brewOrderFailHeat = new("autobrew_orderfail_heatvortex");
        private static readonly Key _brewOrderFailAddSalt = new("autobrew_orderfail_addsalt");
        private static readonly Key _brewOrderFailEffect = new("autobrew_orderfail_addeffect");

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
            ABSettings.GetInt("BrewOrder", data, "SaltIngBase", out _ingSaltBase, 50, false);
            ABSettings.GetInt("BrewOrder", data, "SaltGrindBase", out _grindSaltBase, 5, false);
            ABSettings.GetInt("BrewOrder", data, "SaltStirBase", out _stirSaltBase, 5, false);
            ABSettings.GetInt("BrewOrder", data, "SaltPourBase", out _pourSaltBase, 5, false);
            ABSettings.GetInt("BrewOrder", data, "SaltHeatBase", out _heatSaltBase, 50, false);
            ABSettings.GetInt("BrewOrder", data, "SaltSaltBase", out _saltSaltBase, 5, false);
            ABSettings.GetInt("BrewOrder", data, "SaltEffectBase", out _effectSaltBase, 50, false);
            ABSettings.GetFloat("BrewOrder", data, "SaltGrindMult", out _grindSaltMult, 10f, false);
            ABSettings.GetFloat("BrewOrder", data, "SaltStirMult", out _stirSaltMult, 10f, false);
            ABSettings.GetFloat("BrewOrder", data, "SaltPourMult", out _pourSaltMult, 10f, false);
            ABSettings.GetFloat("BrewOrder", data, "SaltHeatMult", out _heatSaltMult, 0f, false);
            ABSettings.GetFloat("BrewOrder", data, "SaltSaltMult", out _saltSaltMult, 0.1f, false);
        }

        public readonly BrewOrderType Stage;
        public readonly double Target;
        public readonly InventoryItem Item;
        public readonly int Version;

        public BrewOrder(BrewOrderType stage, double target = 0f, int version = -1, InventoryItem item = null)
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
                case BrewOrderType.AddIngredient:
                {
                    return _ingSaltBase;
                }
                case BrewOrderType.GrindPercent:
                {
                    return _grindSaltBase + (int)Math.Ceiling(_grindSaltMult * Target);
                }
                case BrewOrderType.StirCauldron:
                {
                    return _stirSaltBase + (int)Math.Ceiling(_stirSaltMult * Target);
                }
                case BrewOrderType.PourSolvent:
                {
                    return _pourSaltBase + (int)Math.Ceiling(_pourSaltMult * Target);
                }
                case BrewOrderType.HeatVortex:
                {
                    return _heatSaltBase + (int)Math.Ceiling(_heatSaltMult * Target);
                }
                case BrewOrderType.AddSalt:
                {
                    return _saltSaltBase + (int)Math.Ceiling(_saltSaltMult * Target);
                }
                case BrewOrderType.AddEffect:
                {
                    return _effectSaltBase;
                }
            }
            return 100;
        }

        public Key GetFailKey()
        {
            switch (Stage)
            {
                case BrewOrderType.AddIngredient:
                {
                    return _brewOrderFailAddIng;
                }
                case BrewOrderType.GrindPercent:
                {
                    return _brewOrderFailGrind;
                }
                case BrewOrderType.StirCauldron:
                {
                    return _brewOrderFailStir;
                }
                case BrewOrderType.PourSolvent:
                {
                    return _brewOrderFailPour;
                }
                case BrewOrderType.HeatVortex:
                {
                    return _brewOrderFailHeat;
                }
                case BrewOrderType.AddSalt:
                {
                    return _brewOrderFailAddSalt;
                }
                case BrewOrderType.AddEffect:
                {
                    return _brewOrderFailEffect;
                }
                default:
                {
                    return null;
                }
            }
        }

        public override string ToString()
        {
            return $"{{ Order: '{Stage,15}', Version: '{Version,2}', Item: '{Item?.name,15}', Target: '{Target}' }}";
        }
    }
}
