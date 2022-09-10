using AutoBrew.Extensions;
using HarmonyLib;
using PotionCraft.Core.Extensions;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Cauldron;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal class CauldronOverseer : BaseOverseer
    {
        private float _tolerance;
        private Vector3 _pidValues;
        private PIDController _pidControl;
        private float _speedMin;
        private float _speedMax;
        
        private BrewOrder _order;
        private float _stirredTotal;
        private double _lastPIDVal;

        public override void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "Tolerance", out _tolerance, 0.0001f, false);
            ABSettings.GetVector3(nameof(CauldronOverseer), data, "PIDValues", out _pidValues, new Vector3(0.075f, 0.001f, 0.05f));
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "SpeedMin", out _speedMin, 0.5f);
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "SpeedMax", out _speedMax, 0.5f);
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
            _pidControl = new(_pidValues);
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
            float diff = (float)_order.Target - _stirredTotal;
            if (diff <= _tolerance)
            {
                Stage = OverseerStage.Complete;
                bowl.StirringValue = 0f;
                return;
            }

            _lastPIDVal = _pidControl.GetStep(_order.Target, _stirredTotal, Time.deltaTime);
            bowl.StirringValue = (float)_lastPIDVal.Clamp(_speedMin, _speedMax);
        }

        public void AddSpoonAmount(float value, float multiplier)
        {
            if (Stage == OverseerStage.Active)
            {
                if (value.Is(0f))
                {
                    return;
                }
                float delta = (value / multiplier);
                _stirredTotal += delta;
                double clampPID = _lastPIDVal.Clamp(_speedMin, _speedMax);
                Log.LogDebug($"StirUpdate: PIDVal - {_lastPIDVal:N5} | ClampPID - {clampPID:N5} | Delta - {delta:N5}");
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
