using AutoBrew.Extensions;
using AutoBrew.PlotterConverter;
using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.RecipeMap;
using PotionCraft.ObjectBased.Bellows;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal class BellowsOverseer : BaseOverseer
    {
        private static float _sparkAmount;
        private static float _vortexBellowsDuration;
        private static float _effectBellowsDuration;
        private static float _bellowsMinMin;

        private static float _bellowsMin = 331.5f;
        private static float _bellowsMax = 352.5f;
        private static float _bellowsRange;

        private static float _tolerance;
        private static Vector3 _pidValues;
        private static float _heatMin;
        private static float _heatMax;
        
        private BrewOrder _order;
        private PIDController _pidControl;
        private double _heatedTotal;
        private double _heatTarget;
        private Vector2 _vortexPos;
        private Vector2 _indicStartOffset;
        private float _lastAngle;
        private int _fullRotCount;
        private float _gtEffectStart;
        private int _effectTier;
        private double _lastPIDVal;
        private float _lastHeat;
        private bool _wasTeleporting;

        private bool _bellowsActive;
        private float _bellowsMinMax;
        private float _bellowsProgress;

        public static void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "SparkAmount", out _sparkAmount, 10f, false);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "SparkIntervalOn", out _vortexBellowsDuration, 1.6f, false);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "HeatEffectTime", out _effectBellowsDuration, 2.0f, false);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "Tolerance", out _tolerance, 0.5f, false);
            ABSettings.GetVector3(nameof(BellowsOverseer), data, "PIDValues", out _pidValues, new Vector3(0.005f, 0.00001f, 0.0005f));
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "HeatMin", out _heatMin, 0.01f);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "HeatMax", out _heatMax, 0.8f);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "BellowsMinMin", out _bellowsMinMin, 335f);
        }

        public override void Reset()
        {
            _order = null;
            _heatedTotal = 0f;

            Stage = OverseerStage.Idle;
        }

        public override void Setup(BrewOrder order)
        {
            _order = order;
            _heatedTotal = 0f;
            _pidControl = new(_pidValues);
            _wasTeleporting = false;
            _bellowsActive = false;
            _bellowsRange = _bellowsMax - _bellowsMin;

            switch (order.Stage)
            {
                case BrewOrderType.HeatVortex:
                {
                    if (Managers.RecipeMap.CurrentVortexMapItem == null)
                    {
                        Stage = OverseerStage.Failed;
                        return;
                    }
                    _vortexPos = Managers.RecipeMap.CurrentVortexMapItem.transform.localPosition;
                    Vector2 indicPos = Managers.RecipeMap.recipeMapObject.indicatorContainer.localPosition;
                    _indicStartOffset = indicPos - _vortexPos;
                    _lastAngle = 0f;
                    _fullRotCount = 0;

                    if (order.Version != -1)
                    {
                        double length = order.Target;
                        int version = order.Version;
                        PlotterVortex plotter = new(_indicStartOffset, version);
                        _heatTarget = plotter.ConvertHeatVortexLengthToDegrees(length, out _);
                    }
                    else
                    {
                        _heatTarget = order.Target;
                    }
                    break;
                }
                case BrewOrderType.AddEffect:
                {
                    _gtEffectStart = Time.time;
                    break;
                }
            }
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
                Log.LogError("BellowsOverseer is inactive");
                return;
            }

            switch (_order.Stage)
            {
                case BrewOrderType.HeatVortex:
                {
                    Vector2 indicCurrentPos = Managers.RecipeMap.recipeMapObject.indicatorContainer.localPosition;
                    Vector2 indicOffset = indicCurrentPos - _vortexPos;
                    Log.LogInfo($"HeatStatus: Rotation - {_heatedTotal} | StartPos - {_indicStartOffset} | EndPos - {indicOffset} | Magnitude - {indicOffset.magnitude}");
                    return;
                }
                case BrewOrderType.AddEffect:
                {
                    Log.LogInfo($"HeatStatus: {Managers.Ingredient.coals.Heat:P2}");
                    return;
                }
            }
        }

        public override double Accuracy
        {
            get
            {
                switch(_order.Stage)
                {
                    case BrewOrderType.HeatVortex:
                    {
                        // degree (unicode): \u00B0
                        if (_heatTarget == 0f)
                        {
                            return 1.0;
                        }
                        else
                        {
                            return _heatedTotal / _heatTarget;
                        }
                    }
                    case BrewOrderType.AddEffect:
                    {
                        if (_order.Target == 0f)
                        {
                            return 1.0;
                        }
                        return _effectTier / _order.Target;
                    }
                }
                return 0.0;
            }
        }

        public override double Precision
        {
            get
            {
                switch (_order.Stage)
                {
                    case BrewOrderType.HeatVortex: { return _heatTarget - _heatedTotal; }
                    default: { return 0; }
                }
            }
        }

        public void Heat_Update()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            UpdateBellowsRotation();
            switch (_order.Stage)
            {
                case BrewOrderType.HeatVortex:
                {
                    double diff = Math.Abs(_heatTarget) - Math.Abs(_heatedTotal);
                    if (diff <= _tolerance)
                    {
                        if (_wasTeleporting && !Managers.RecipeMap.indicator.IsIndicatorTeleporting())
                        {
                            Stage = OverseerStage.Complete;
                        }
                        Managers.Ingredient.coals.Heat = 0f;
                        return;
                    }

                    _lastPIDVal = Math.Abs(_pidControl.GetStep(_heatTarget, _heatedTotal, Time.deltaTime));
                    _lastHeat = (float)_lastPIDVal.Clamp(_heatMin, _heatMax);
                    Managers.Ingredient.coals.Heat = _lastHeat;
                    Log.LogDebug($"Bellows Heat: {_lastHeat}");
                    return;
                }
                case BrewOrderType.AddEffect:
                {
                    if (_heatedTotal >= 1.5f)
                    {
                        Stage = OverseerStage.Failed;
                        return;
                    }
                    _heatedTotal = (Time.time - _gtEffectStart) / _effectBellowsDuration;
                    Managers.Ingredient.coals.Heat = Mathf.Clamp01((float)_heatedTotal);
                    return;
                }
            }
        }

        public void Sparks_Update(ref float dAngle)
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            if (_bellowsActive)
            {
                dAngle = _sparkAmount;
            }
        }

        public void MoveIndicatorTowardsVortex()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            if (Managers.RecipeMap.indicator.IsIndicatorTeleporting())
            {
                // if potion teleports, assume 100% accuracy
                _heatedTotal = _heatTarget;
                _wasTeleporting = true;
                return;
            }
            else if (_wasTeleporting)
            {
                return;
            }

            Vector2 indicPos = Managers.RecipeMap.recipeMapObject.indicatorContainer.localPosition;
            var indicCurrOffset = indicPos - _vortexPos;
            float angle = Vector2.SignedAngle(_indicStartOffset, indicCurrOffset);
            if ((Mathf.Sign(angle) > 0) && (Mathf.Sign(_lastAngle) < 0))
            {
                _fullRotCount++;
            }

            double newTotal = (_fullRotCount * -360f) + angle;
            double delta = newTotal - _heatedTotal;
            _heatedTotal = newTotal;
            _lastAngle = angle;

            double clampPID = _lastPIDVal.Clamp(_heatMin, _heatMax);
            //Log.LogDebug($"StirUpdate: PIDVal - {_lastPIDVal:N5} | ClampPID - {clampPID:N5} | Delta - {delta:N5}");
        }

        public void CollectEffect(int tier)
        {
            if ((Stage != OverseerStage.Active) || (_order.Stage != BrewOrderType.AddEffect))
            {
                return;
            }

            _effectTier = tier;
            Stage = OverseerStage.Complete;
            Managers.Ingredient.coals.Heat = 0f;
        }

        public void UpdateBellowsRotation()
        {
            if (_wasTeleporting == true)
            {
                _bellowsActive = false;
                return;
            }

            var z = Managers.Ingredient.coals.top.rotation.eulerAngles.z;

            if (!_bellowsActive)
            {
                if (z <= _bellowsMin)
                {
                    _bellowsActive = true;
                    _bellowsProgress = 0f;
                }
            }
            else
            {
                float newZ;
                switch (_order.Stage)
                {
                    case BrewOrderType.HeatVortex:
                    {
                        // we want uniform speed, so lerp between min and max bellows angles
                        _bellowsProgress += Time.deltaTime / _vortexBellowsDuration;
                        newZ = Mathf.Lerp(_bellowsMin, _bellowsMax, _bellowsProgress);

                        // how far the arm rotates should be based on the heat (less heat -> more precision -> smaller movements),
                        // so use the last heat value to get the upper limit (minmax) of the bellows arm rotation
                        float rotCoef = _lastHeat / _heatMax;
                        _bellowsMinMax = _bellowsMin + (rotCoef * _bellowsRange);

                        // raise the floor of the minmax (don't want it to be too low),
                        // then clamp the calculated rotation between the min and minmax
                        _bellowsMinMax = Mathf.Clamp(_bellowsMinMax, _bellowsMinMin, _bellowsMax);
                        newZ = Mathf.Clamp(newZ, _bellowsMin, _bellowsMinMax);
                        break;
                    }
                    case BrewOrderType.AddEffect:
                    {
                        _bellowsProgress += Time.deltaTime / (_effectBellowsDuration * 0.2f);
                        _bellowsMinMax = _bellowsMax;
                        newZ = Mathf.Lerp(_bellowsMin, _bellowsMax, _bellowsProgress);
                        break;
                    }
                    default:
                    {
                        return;
                    }
                }

                Managers.Ingredient.coals.top.transform.eulerAngles = Vector3.forward * newZ;
                if (newZ == _bellowsMinMax)
                {
                    _bellowsActive = false;
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BellowsCoals), "Heat_Update")]
        public static void Heat_Update_Postfix()
        {
            if (!BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Boiler.Heat_Update();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BellowsCoals), "Sparks_Update")]
        public static void Sparks_Update_Prefix(ref float dAngle)
        {
            if (!BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Boiler.Sparks_Update(ref dAngle);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RecipeMapManager), "MoveIndicatorTowardsVortex")]
        public static void MoveIndicatorTowardsVortex_Postfix()
        {
            if (!BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Boiler.MoveIndicatorTowardsVortex();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RecipeMapManager), "CollectEffect")]
        public static void CollectEffect_Postfix(int tier)
        {
            if (!BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Boiler.CollectEffect(tier);
        }
    }
}