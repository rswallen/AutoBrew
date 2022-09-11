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
        private float _sparkAmount;
        private float _sparkIntervalOn;
        private float _sparkIntervalOff;
        private float _heatEffectTime;
        
        private float _tolerance;
        private Vector3 _pidValues;
        private float _heatMin;
        private float _heatMax;
        
        private BrewOrder _mode;
        private PIDController _pidControl;
        private double _gtLastSparkChange;
        private bool _sparksActive;
        private double _heatedTotal;
        private double _heatTarget;
        private Vector2 _vortexPos;
        private Vector2 _indicStartOffset;
        private float _lastAngle;
        private int _fullRotCount;
        private float _gtEffectStart;
        private int _effectTier;
        private double _lastPIDVal;

        public override void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "SparkAmount", out _sparkAmount, 10f, false);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "SparkIntervalOn", out _sparkIntervalOn, 1.6f, false);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "SparkIntervalOff", out _sparkIntervalOff, 0.4f, false);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "HeatEffectTime", out _heatEffectTime, 2.0f, false);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "Tolerance", out _tolerance, 0.5f, false);
            ABSettings.GetVector3(nameof(BellowsOverseer), data, "PIDValues", out _pidValues, new Vector3(0.005f, 0.00001f, 0.0005f));
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "HeatMin", out _heatMin, 0.01f);
            ABSettings.GetFloat(nameof(BellowsOverseer), data, "HeatMax", out _heatMax, 0.8f);
        }

        public override void Reset()
        {
            _mode = null;
            _heatedTotal = 0f;

            Stage = OverseerStage.Idle;
        }

        public override void Setup(BrewOrder order)
        {
            _mode = order;
            _gtLastSparkChange = Time.timeAsDouble;
            _sparksActive = true;
            _heatedTotal = 0f;
            _pidControl = new(_pidValues);

            switch (order.Stage)
            {
                case BrewStage.HeatVortex:
                {
                    if (Managers.RecipeMap.currentVortexMapItem == null)
                    {
                        Stage = OverseerStage.Failed;
                        return;
                    }
                    _vortexPos = Managers.RecipeMap.currentVortexMapItem.transform.localPosition;
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
                case BrewStage.AddEffect:
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

            switch (_mode.Stage)
            {
                case BrewStage.HeatVortex:
                {
                    Vector2 indicCurrentPos = Managers.RecipeMap.recipeMapObject.indicatorContainer.localPosition;
                    Vector2 indicOffset = indicCurrentPos - _vortexPos;
                    Log.LogInfo($"HeatStatus: Rotation - {_heatedTotal} | StartPos - {_indicStartOffset} | EndPos - {indicOffset} | Magnitude - {indicOffset.magnitude}");
                    return;
                }
                case BrewStage.AddEffect:
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
                switch(_mode.Stage)
                {
                    case BrewStage.HeatVortex:
                    {
                        if (_heatTarget == 0f)
                        {
                            return 1.0;
                        }
                        else
                        {
                            return _heatedTotal / _heatTarget;
                        }
                    }
                    case BrewStage.AddEffect:
                    {
                        if (_mode.Target == 0f)
                        {
                            return 1.0;
                        }
                        return _effectTier / _mode.Target;
                    }
                }
                return 0.0;
            }
        }

        public void Heat_Update()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            switch (_mode.Stage)
            {
                case BrewStage.HeatVortex:
                {
                    double diff = Math.Abs(_heatTarget) - Math.Abs(_heatedTotal);
                    if (diff <= _tolerance)
                    {
                        Stage = OverseerStage.Complete;
                        Managers.Ingredient.coals.Heat = 0f;
                        return;
                    }

                    _lastPIDVal = Math.Abs(_pidControl.GetStep(_heatTarget, _heatedTotal, Time.deltaTime));
                    Managers.Ingredient.coals.Heat = (float)_lastPIDVal.Clamp(_heatMin, _heatMax);
                    return;
                }
                case BrewStage.AddEffect:
                {
                    if (_heatedTotal >= 1.5f)
                    {
                        Stage = OverseerStage.Failed;
                        return;
                    }
                    _heatedTotal = (Time.time - _gtEffectStart) / _heatEffectTime;
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

            switch (_mode.Stage)
            {
                case BrewStage.HeatVortex:
                {
                    float interval = _sparksActive ? _sparkIntervalOn : _sparkIntervalOff;
                    double gtNextChange = _gtLastSparkChange + interval;
                    if (gtNextChange < Time.timeAsDouble)
                    {
                        _sparksActive = !_sparksActive;
                        _gtLastSparkChange = Time.timeAsDouble;
                    }
                    dAngle = _sparksActive ? _sparkAmount : 0f;
                    return;
                }
                case BrewStage.AddEffect:
                {
                    dAngle = _sparkAmount;
                    return;
                }
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
                Stage = OverseerStage.Complete;
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
            Log.LogDebug($"StirUpdate: PIDVal - {_lastPIDVal:N5} | ClampPID - {clampPID:N5} | Delta - {delta:N5}");
        }

        public void CollectEffect(int tier)
        {
            if ((Stage != OverseerStage.Active) || (_mode.Stage != BrewStage.AddEffect))
            {
                return;
            }

            _effectTier = tier;
            Stage = OverseerStage.Complete;
            Managers.Ingredient.coals.Heat = 0f;
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