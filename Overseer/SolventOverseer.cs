using AutoBrew.Extensions;
using HarmonyLib;
using PotionCraft.Core.Extensions;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Ladle;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal class SolventOverseer : BaseOverseer
    {
        private static double _tolerance;
        private static Vector3 _pidValues;
        private static double _anglePourStart;
        private static double _anglePourReset;
        private static double _anglePourMax;

        private PIDController _pidControl;
        private double _pourTarget;
        private double _pouredTotal;
        private double _lastPIDVal;

        public static void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetDouble(nameof(SolventOverseer), data, "Tolerance", out _tolerance, 0.0001, false);
            ABSettings.GetVector3(nameof(SolventOverseer), data, "PIDValues", out _pidValues, new(50.0f, 0.1f, 0.8f), false);
            ABSettings.GetDouble(nameof(SolventOverseer), data, "AngleStart", out _anglePourStart, -20.0, false);
            ABSettings.GetDouble(nameof(SolventOverseer), data, "AngleReset", out _anglePourReset, -16.5, false);
            ABSettings.GetDouble(nameof(SolventOverseer), data, "AngleMax", out _anglePourMax, -40.0, false);
        }

        public override void Reset()
        {
            _pourTarget = 0f;
            _pouredTotal = 0f;

            Stage = OverseerStage.Idle;
        }

        public override void Setup(BrewOrder order)
        {
            _pourTarget = order.Target;
            _pouredTotal = 0f;
            _pidControl = new(_pidValues);
            base.Setup(order);
        }
        
        public override void Process()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            float distance = Managers.RecipeMap.GetDistanceToBase();
            float rotation = Managers.RecipeMap.currentMap.potionBaseMapItem.thisTransform.eulerAngles.z - Managers.RecipeMap.indicatorRotation.VisualValue;
            rotation = rotation.RecalculateEulerAngle(FloatExtension.AngleType.MinusPiToPi);
            if (distance.Is(0f) && rotation.Is(0f))
            {
                Stage = OverseerStage.Complete;
                return;
            }

            double diff = _pourTarget - _pouredTotal;
            if (diff <= _tolerance)
            {
                Stage = Stage = OverseerStage.Complete;
                return;
            }
        }

        public override void LogStatus()
        {
            if (Idle)
            {
                Log.LogError("SolventOverseer is inactive");
                return;
            }

            if (_pourTarget.Is(0f))
            {
                Log.LogInfo($"PourComplete: NaN% | {_pouredTotal}/0");
                return;
            }

            double percent = _pouredTotal / _pourTarget;
            Log.LogInfo($"PourStatus: {percent:P8} | {_pouredTotal:N8}/{_pourTarget:N8}");
        }
        
        public override double Accuracy
        {
            get
            {
                if (_pourTarget == 0f)
                {
                    return 1.0;
                }
                else
                {
                    return _pouredTotal / _pourTarget;
                }
            }
        }

        public override double Precision
        {
            get { return Math.Abs(_pouredTotal - _pourTarget); }
        }

        public void AddLadleAmount(float value, float multiplier)
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            if (value.Is(0f))
            {
                return;
            }
            _pouredTotal += (value / multiplier);
        }

        private void UpdateWaterLevel(WaterStream instance)
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            double actualMin = instance.isPouring ? _anglePourReset : _anglePourStart;
            _lastPIDVal = _pidControl.GetStep(_pourTarget, _pouredTotal, Time.deltaTime);
            // max and min angles are -ve, so they are in the "wrong" place
            double target = actualMin - _lastPIDVal;
            double clamped = target.Clamp(_anglePourMax, actualMin);
            //instance.ladle.rotatablePart.localRotation = Quaternion.Euler((float)clamped * Vector3.forward);
            instance.ladle.rotatablePart.localEulerAngles = (float)clamped * Vector3.forward;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(WaterStream), "UpdateWaterLevel")]
        public static void UpdateWaterLevel_Prefix(WaterStream __instance)
        {
            if (!BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Pourer.UpdateWaterLevel(__instance);
        }
    }
}
