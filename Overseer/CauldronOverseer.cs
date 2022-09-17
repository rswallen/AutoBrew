using AutoBrew.Extensions;
using HarmonyLib;
using PotionCraft.Core.Extensions;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Cauldron;
using PotionCraft.ObjectBased.Spoon;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal class CauldronOverseer : BaseOverseer
    {
        private static double _tolerance;
        private static Vector3 _pidValues;
        private static double _speedMin;
        private static double _speedMax;
        private static double _intervalMaxUpdate;
        private static Vector2 _stirTotalScalar;
        private static Vector2 _spoonPosScalar;
        private static Vector2 _spoonPosOffset;
        private static Vector3 _spoonRotScalar;

        private BrewOrder _order;
        private PIDController _pidControl;
        private double _stirredTotal;
        private double _lastPIDVal;
        private double _gtLastUpdate;

        public static void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetDouble(nameof(CauldronOverseer), data, "Tolerance", out _tolerance, 0.0001f, false);
            ABSettings.GetVector3(nameof(CauldronOverseer), data, "PIDValues", out _pidValues, new Vector3(0.075f, 0.001f, 0.05f));
            ABSettings.GetDouble(nameof(CauldronOverseer), data, "SpeedMin", out _speedMin, 0.001f);
            ABSettings.GetDouble(nameof(CauldronOverseer), data, "SpeedMax", out _speedMax, 0.8f);
            ABSettings.GetDouble(nameof(CauldronOverseer), data, "MaxUpdateInterval", out _intervalMaxUpdate, 1.0f);
            ABSettings.GetVector2(nameof(CauldronOverseer), data, "StirTotalScalar", out _stirTotalScalar, new Vector2(2.0f, 0.2f));
            ABSettings.GetVector2(nameof(CauldronOverseer), data, "SpoonPosScalar", out _spoonPosScalar, new Vector2(2.0f, 0.2f));
            ABSettings.GetVector2(nameof(CauldronOverseer), data, "SpoonPosOffset", out _spoonPosOffset, new Vector2(0.0f, 3.0f));
            ABSettings.GetVector3(nameof(CauldronOverseer), data, "SpoonRotScalar", out _spoonRotScalar, new Vector3(0.0f, 0.0f, -10.0f));
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
            _stirredTotal = 0;
            _pidControl = new(_pidValues);
            _gtLastUpdate = Time.timeAsDouble;
            base.Setup(order);
        }

        public override void Process()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }
            UpdateSpoonPos();

            double interval = Time.timeAsDouble - _gtLastUpdate;
            if (interval >= _intervalMaxUpdate)
            {
                Stage = OverseerStage.Complete;
                return;
            }
        }

        public override void LogStatus()
        {
            if (Idle)
            {
                Log.LogError("CauldronOverseer is inactive");
                return;
            }

            if (_order.Target.Is(0.0))
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
                if (_order.Target == 0.0)
                {
                    return 1.0;
                }
                else
                {
                    return _stirredTotal / _order.Target;
                }
            }
        }

        public override double Precision
        {
            get { return Math.Abs(_order.Target - _stirredTotal); }
        }

        private void UpdateSpoonPos()
        {
            Spoon stirrer = Managers.Ingredient.spoon;
            Cauldron bowl = Managers.Ingredient.cauldron;
            if ((stirrer == null) || (bowl == null))
            {
                return;
            }

            // we want X and Y to move at different speeds, so scalar * stirtotal
            var scaledTotal = _stirTotalScalar * (float)_stirredTotal;
            // x = sin(total), y = cos(total)
            var trigScaledTotal = new Vector2(Mathf.Sin(scaledTotal.x), Mathf.Cos(scaledTotal.y));
            // scale to set the bounds of movement
            var trigOffset = Vector2.Scale(_spoonPosScalar, trigScaledTotal);
            // final pos = cauldron.pos + origin offset that spoon moves around + movement
            var spoonOrigin = (Vector2)bowl.transform.position + _spoonPosOffset;
            var position = spoonOrigin + trigOffset;
            // we want rotation change to match horizontal movement, so rotScalar * trigscalar.x
            var rotation = trigScaledTotal.x * _spoonRotScalar;
            stirrer.MoveToInstantly(position, rotation);
        }

        public void UpdateStirringValue()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            float pathLeft = Managers.RecipeMap.path.GetFixedPathLength();
            if (pathLeft.Is(0f))
            {
                Stage = OverseerStage.Complete;
                return;
            }

            Cauldron bowl = Managers.Ingredient.cauldron;
            double diff = _order.Target - _stirredTotal;
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
                _gtLastUpdate = Time.timeAsDouble;
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