using AutoBrew.Extensions;
using HarmonyLib;
using PotionCraft.Core.Extensions;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Ladle;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal class SolventOverseer : BaseOverseer
    {
        private float _pourTolerance;
        private float _pourThreshSlow;
        private float _pourFastAngle;
        private float _pourSlowAngle;
        private float _pourSlowSpeed;
        private float _lerpDuration;

        PourStage _pStage;
        double _pourTarget;
        double _pouredTotal;

        bool _resetLadle;
        float _progress;
        float _startZ;
        float _currentZ;

        public override void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetFloat(nameof(SolventOverseer), data, "PourTolerance", out _pourTolerance, 1.0f, false);
            ABSettings.GetFloat(nameof(SolventOverseer), data, "PourThreshSlow", out _pourThreshSlow, 1.0f, false);
            ABSettings.GetFloat(nameof(SolventOverseer), data, "PourFastAngle", out _pourFastAngle, 320f, false);
            ABSettings.GetFloat(nameof(SolventOverseer), data, "PourSlowAngle", out _pourSlowAngle, 340f, false);
            ABSettings.GetFloat(nameof(SolventOverseer), data, "PourSlowSpeed", out _pourSlowSpeed, 0.01f, false);
            ABSettings.GetFloat(nameof(SolventOverseer), data, "LerpDuration", out _lerpDuration, 0.2f, false);
        }

        public override void Reset()
        {
            _resetLadle = true;
            _pourTarget = 0f;
            _pouredTotal = 0f;
            _currentZ = 0f;

            Stage = OverseerStage.Idle;
        }

        public override void Setup(BrewOrder order)
        {
            _pourTarget = order.Target;
            _pouredTotal = 0f;
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
            if (distance.Is(0f) && rotation.Is(0f))
            {
                _pStage = PourStage.Reset;
                return;
            }

            double diff = _pourTarget - _pouredTotal;
            if (diff <= _pourTolerance)
            {
                _pStage = PourStage.Reset;
                return;
            }
            else if (diff <= _pourThreshSlow)
            {
                if (_pStage != PourStage.Slow)
                {
                    _resetLadle = true;
                }
                _pStage = PourStage.Slow;
            }
            else
            {
                if (_pStage != PourStage.Fast)
                {
                    _resetLadle = true;
                }
                _pStage = PourStage.Fast;
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

        public void AddLadleAmount(float value, float multiplier)
        {
            if (Stage == OverseerStage.Active)
            {
                if (value.Is(0f))
                {
                    return;
                }
                _pouredTotal += (value / multiplier);
            }
        }

        private void UpdateWaterLevel(WaterStream instance)
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            Quaternion target = Quaternion.Euler(0f * Vector3.forward);
            switch (_pStage)
            {
                case PourStage.Fast:
                {
                    target = Quaternion.Euler(_pourFastAngle * Vector3.forward);
                    break;
                }
                case PourStage.Slow:
                {
                    target = Quaternion.Euler(_pourSlowAngle * Vector3.forward);
                    break;
                }
                case PourStage.Reset:
                {
                    instance.ladle.rotatablePart.localRotation = target;
                    return;
                }
            }

            if (_resetLadle)
            {
                _startZ = _currentZ;
                _progress = 0f;
                _resetLadle = false;
            }

            _progress += Mathf.Clamp01(Time.deltaTime / _lerpDuration);
            if (_progress.Is(1f))
            {
                _progress = 1f;
            }
            var lerped = Quaternion.Lerp(Quaternion.Euler(_startZ * Vector3.forward), target, _progress);
            _currentZ = lerped.eulerAngles.z;
            _currentZ += (_currentZ < 0) ? 360f : 0;
            instance.ladle.rotatablePart.localRotation = Quaternion.Euler(_currentZ * Vector3.forward);
        }

        private void MoveIndicatorTowardsBase(WaterStream instance)
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            switch (_pStage)
            {
                case PourStage.Slow:
                {
                    instance.Pouring = _pourSlowSpeed;
                    instance.isPouring = true;
                    break;
                }
                case PourStage.Reset:
                {
                    instance.Pouring = 0f;
                    instance.isPouring = false;
                    Stage = OverseerStage.Complete;
                    break;
                }
            }
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

        [HarmonyPrefix, HarmonyPatch(typeof(WaterStream), "MoveIndicatorTowardsBase")]
        public static void MoveIndicatorTowardsBase_Prefix(WaterStream __instance)
        {
            if (!BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Pourer.MoveIndicatorTowardsBase(__instance);
        }

        enum PourStage
        {
            Fast,
            Slow,
            Reset
        }
    }
}
