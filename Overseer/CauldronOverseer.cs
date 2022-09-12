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
        private float _tolerance;
        private Vector3 _pidValues;
        private float _speedMin;
        private float _speedMax;
        private Vector2 _stirTotalScalar;
        private Vector2 _spoonPosScalar;
        private Vector2 _spoonPosOffset;
        private Vector3 _spoonRotScalar;

        private BrewOrder _order;
        private PIDController _pidControl;
        private float _stirredTotal;
        private double _lastPIDVal;

        public override void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "Tolerance", out _tolerance, 0.0001f, false);
            ABSettings.GetVector3(nameof(CauldronOverseer), data, "PIDValues", out _pidValues, new Vector3(0.075f, 0.001f, 0.05f));
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "SpeedMin", out _speedMin, 0.001f);
            ABSettings.GetFloat(nameof(CauldronOverseer), data, "SpeedMax", out _speedMax, 0.8f);
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
            _stirredTotal = 0f;
            _pidControl = new(_pidValues);
            base.Setup(order);
        }

        public override void Process()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }
            UpdateSpoonPos();
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

        private void UpdateSpoonPos()
        {
            Spoon stirrer = Managers.Ingredient.spoon;
            Cauldron bowl = Managers.Ingredient.cauldron;
            if ((stirrer == null) || (bowl == null))
            {
                return;
            }

            // we want X and Y to move at different speeds, so scalar * stirtotal
            var scaledTotal = _stirTotalScalar * _stirredTotal;
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