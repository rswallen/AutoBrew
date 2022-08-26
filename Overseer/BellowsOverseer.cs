using AutoBrew.PlotterConverter;
using HarmonyLib;
using PotionCraft.Core.Extensions;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.RecipeMap;
using PotionCraft.ObjectBased.Bellows;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal class BellowsOverseer : BaseOverseer
    {
        private float _sparkAmount;
        private float _sparkIntervalOn;
        private float _sparkIntervalOff;
        private float _heatFast;
        private float _heatSlow;
        private float _heatThreshSlow;
        private float _heatThreshStop;
        private float _heatEffectTime;

        private BrewOrder _mode;
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

        public override void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.SetOrigin("BellowsOverseer");
            ABSettings.GetFloat(data, "SparkAmount", out _sparkAmount, 10f, false);
            ABSettings.GetFloat(data, "SparkIntervalOn", out _sparkIntervalOn, 1.6f, false);
            ABSettings.GetFloat(data, "SparkIntervalOff", out _sparkIntervalOff, 0.4f, false);
            ABSettings.GetFloat(data, "HeatFast", out _heatFast, 0.25f, false);
            ABSettings.GetFloat(data, "HeatSlow", out _heatSlow, 0.05f, false);
            ABSettings.GetFloat(data, "HeatThreshSlow", out _heatThreshSlow, 45.0f, false);
            ABSettings.GetFloat(data, "HeatThreshStop", out _heatThreshStop, 0.5f, false);
            ABSettings.GetFloat(data, "HeatEffectTime", out _heatEffectTime, 2.0f, false);
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
            Stage = OverseerStage.Active;
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
                    double diff = Math.Abs(_heatTarget - _heatedTotal);
                    if (diff <= _heatThreshStop)
                    {
                        Stage = OverseerStage.Complete;
                        Managers.Ingredient.coals.Heat = 0f;
                        return;
                    }
                    else if (diff <= _heatThreshSlow)
                    {
                        Managers.Ingredient.coals.Heat = _heatSlow;
                    }
                    else
                    {
                        Managers.Ingredient.coals.Heat = _heatFast;
                    }
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

            _heatedTotal = (_fullRotCount * -360f) + angle;
            _lastAngle = angle;
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