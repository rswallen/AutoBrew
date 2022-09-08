using AutoBrew.Extensions;
using HarmonyLib;
using PotionCraft.Core.Extensions;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Cauldron;
using System.Collections.Generic;

namespace AutoBrew.Overseer
{
    internal class CauldronOverseer : BaseOverseer
    {
        private float _stirTolerance;
        private float _stirThreshSlow;
        private float _stirFast;
        private float _stirSlow;

        private BrewOrder _order;
        private float _stirredTotal;

        public override void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "StirTolerance", out _stirTolerance, 0.0001f, false);
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "StirThreshSlow", out _stirThreshSlow, 0.5f, false);
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "StirFast", out _stirFast, 0.2f, false);
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "StirSlow", out _stirSlow, 0.05f, false);
        }

        public override void Reset()
        {
            _order = null;
            _stirredTotal = 0f;
            Stage = OverseerStage.Idle;
        }

        public override void Setup(BrewOrder order)
        {
            _order = order;
            _stirredTotal = 0f;
            base.Setup(order);
        }

        public override void Process()
        {
            // nothing happens here, because its all handled through patches
        }

        public override void LogStatus()
        {
            if (Idle)
            {
                Log.LogError("CauldronOverseer is inactive");
                return;
            }

            if (_order.Target.Is(0f))
            {
                Log.LogInfo($"StirStatus: NaN | {_stirredTotal}/0");
                return;
            }

            double percent = _stirredTotal / _order.Target;
            Log.LogInfo($"StirStatus: {percent:P8}% | {_stirredTotal:N8}/{_order.Target:N8}");
        }

        public override double Accuracy
        {
            get
            {
                if (_order.Target == 0f)
                {
                    return 1.0;
                }
                else
                {
                    return _stirredTotal / _order.Target;
                }
            }
        }

        public void UpdateStirringValue()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            if (Managers.RecipeMap.path.GetFixedPathLength().Is(0f))
            {
                Stage = OverseerStage.Complete;
                return;
            }

            Cauldron bowl = Managers.Ingredient.cauldron;
            double diff = _order.Target - _stirredTotal;
            if (diff <= _stirTolerance)
            {
                Stage = OverseerStage.Complete;
                bowl.StirringValue = 0f;
            }
            else if (diff <= _stirThreshSlow)
            {
                bowl.StirringValue = _stirSlow;
            }
            else
            {
                bowl.StirringValue = _stirFast;
            }
        }

        public void AddSpoonAmount(float value, float multiplier)
        {
            if (Stage == OverseerStage.Active)
            {
                if (value.Is(0f))
                {
                    return;
                }
                _stirredTotal += (value / multiplier);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Cauldron), "UpdateStirringValue")]
        public static void UpdateStirringValue_Postfix()
        {
            if (!BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Stirrer.UpdateStirringValue();
        }
    }
}
