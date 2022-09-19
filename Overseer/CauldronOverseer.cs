using AutoBrew.Extensions;
using HarmonyLib;
using PotionCraft.Core.Extensions;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.RecipeMap;
using PotionCraft.ObjectBased.Cauldron;
using PotionCraft.ObjectBased.RecipeMap.Path;
using PotionCraft.ObjectBased.RecipeMap.RecipeMapItem.Zones;
using PotionCraft.ObjectBased.Spoon;
using PotionCraft.Settings;
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
        private static double _swampSpeedScalar;
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
        private bool _inSwamp;

        public static void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetDouble(nameof(CauldronOverseer), data, "Tolerance", out _tolerance, 0.0001f, false);
            ABSettings.GetVector3(nameof(CauldronOverseer), data, "PIDValues", out _pidValues, new Vector3(0.075f, 0.001f, 0.05f));
            ABSettings.GetDouble(nameof(CauldronOverseer), data, "SpeedMin", out _speedMin, 0.001f);
            ABSettings.GetDouble(nameof(CauldronOverseer), data, "SpeedMax", out _speedMax, 0.8f);
            ABSettings.GetDouble(nameof(CauldronOverseer), data, "SwampSpeedScalar", out _swampSpeedScalar, 0.6f);
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
            _inSwamp = false;
            base.Setup(order);
        }

        public override void Process()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            int fixedCount = Managers.RecipeMap.path.fixedPathHints?.Count ?? 0;
            if (fixedCount > 0)
            {
                var hint = Managers.RecipeMap.path.fixedPathHints[0];
                if (hint is TeleportationFixedHint teleHint)
                {
                    if (teleHint.isIndicatorMovingAlongPath)
                    {
                        // 1 - movingalongpathstatus = percent remaining
                        // percent remaining * total length of path = distance remaining
                        // teleport speed * deltaTime = (speed * time = distance) = distance moved in deltatime interval
                        // clamp frameDistance between 0 and distance remaining and add it to stirred total
                        // record that we made an update

                        float percentLeft = 1f - teleHint.MovingAlongPathStatus;
                        float distanceLeft = percentLeft * teleHint.graphicsPathLengthOnTeleportationAnimationStart;
                        float frameDistance = Managers.RecipeMap.indicator.teleportationAnimator.GetMovingSpeed() * Time.deltaTime;
                        _stirredTotal += Mathf.Clamp(frameDistance, 0f, distanceLeft);
                        _gtLastUpdate = Time.timeAsDouble;
                    }
                }
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
            _inSwamp = (ZonePart.GetZonesActivePartsCount(typeof(SwampZonePart)) > 0);
            _lastPIDVal = _pidControl.GetStep(_order.Target, _stirredTotal, Time.deltaTime);
            double max = _speedMax * (_inSwamp ? _swampSpeedScalar : 1.0);
            bowl.StirringValue = (float)_lastPIDVal.Clamp(_speedMin, max);
        }

        public void AddSpoonAmount(float value, float multiplier)
        {
            if (Stage == OverseerStage.Active)
            {
                if (value.Is(0f))
                {
                    return;
                }

                int fixedCount = Managers.RecipeMap.path.fixedPathHints?.Count ?? 0;
                if (fixedCount > 0)
                {
                    var hint = Managers.RecipeMap.path.fixedPathHints[0];
                    if (hint is TeleportationFixedHint teleHint)
                    {
                        if (teleHint.isIndicatorMovingAlongPath)
                        {
                            // we only get one mark added at the start of using a crystal path
                            // but as we do the tracking elsewhere, ignore it
                            Log.LogInfo("Ignoring teleport mark");
                            return;
                        }
                    }
                }

                float delta = (value / multiplier);
                delta *= _inSwamp ? (1 - Settings<RecipeMapManagerIndicatorSettings>.Asset.indicatorInSwampPathDeletion) : 1f;
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