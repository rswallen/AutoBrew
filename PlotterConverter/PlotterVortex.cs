using PotionCraft.Core.Extensions;
using PotionCraft.ManagersSystem.RecipeMap;
using PotionCraft.Settings;
using UnityEngine;

namespace AutoBrew.PlotterConverter
{
    internal class PlotterVortex
    {
        public static bool IsValidVersion(int version)
        {
            switch (version)
            {
                case 0:
                case 1:
                {
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        private readonly int _version;
        private readonly Vector2 _startOffset;
        private readonly float _heatRot;
        private readonly RecipeMapManagerVortexSettings _asset;
        public Vector2 CurrentOffset;

        public PlotterVortex(Vector2 startOffset, int version)
        {
            _startOffset = startOffset;
            _asset = Settings<RecipeMapManagerVortexSettings>.Asset;
            _heatRot = 0.19f * _asset.vortexMovementSpeed * (1.0f / 60f);
            _version = version;
        }

        public double ConvertHeatVortexLengthToDegrees(double target, out int counter)
        {
            bool teleport = false;
            double length = 0.0f;
            float lastAngle = 0f;
            int fullRotCount = 0;
            CurrentOffset = _startOffset;

            for (counter = 0; length < target; counter++)
            {
                if (!GetNextPos(out Vector2 nextPos))
                {
                    teleport = true;
                    break;
                }
                float angle = Vector2.SignedAngle(_startOffset, nextPos);
                if (Mathf.Sign(angle) > 0 && Mathf.Sign(lastAngle) < 0)
                {
                    fullRotCount++;
                }

                length += _version == 1 ? (nextPos - CurrentOffset).magnitude : _heatRot;
                CurrentOffset = nextPos;
                lastAngle = angle;
            }

            if (teleport)
            {
                return -800f * Mathf.Sign(_asset.vortexSpiralStep);
            }
            return fullRotCount * -360.0 * Mathf.Sign(_asset.vortexSpiralStep) + lastAngle;
        }

        // from RecipeMapManager.MoveIndicatorTowardsVortex
        public bool GetNextPos(out Vector2 nextPos)
        {
            // dislacement of vortex from indicator
            float magnitude = CurrentOffset.magnitude;
            nextPos = Vector2.zero;

            //if indicator is really close to vortex centre
            if (magnitude.Is(0f, 0.001f))
            {
                return false;
            }

            // directional unit step size
            float unitStep = Mathf.Sign(_asset.vortexSpiralThetaPower) * _asset.vortexSpiralStep;

            // unit phi = (r * 2pi / stepsize) ^ 1/theta
            float unitRot = Mathf.Pow(magnitude * 2f * 3.1415927f / unitStep, 1f / _asset.vortexSpiralThetaPower);

            // heat based step
            float deltaRot = unitRot - _heatRot * ((int)Mathf.Sign(_asset.vortexSpiralThetaPower) * (int)Mathf.Sign(unitStep));

            //if rotation is negligable, num4 is 0
            // else clamp num4 to 0 if its opposite sign of unitRot
            deltaRot = unitRot.Is(0f) ? 0f : unitRot < 0f ? Mathf.Min(0f, deltaRot) : Mathf.Max(0f, deltaRot);

            // newMag = magnitude of vector for new pos once reduced by heat step (r = step * phi^thetaPower / (2 * pi)
            float deltaMag = unitStep * 0.5f / 3.1415927f * Mathf.Pow(deltaRot, _asset.vortexSpiralThetaPower);

            // point that is distance "magnitude" from vortex and rotated num3 rads from zero
            Vector2 unitPos = magnitude * new Vector2(Mathf.Cos(unitRot), Mathf.Sin(unitRot));

            // point that is distance "d" from vortex and rotated num4 rads from zero
            Vector2 deltaPos = deltaMag * new Vector2(Mathf.Cos(deltaRot), Mathf.Sin(deltaRot));

            // rotate vec3 by angle between from and to
            deltaPos = deltaPos.Rotate(Vector2.SignedAngle(unitPos, CurrentOffset));

            // move from indicator pos to vector3 by 
            nextPos = Vector2.MoveTowards(CurrentOffset, deltaPos, _heatRot);
            return true;
        }
    }
}
