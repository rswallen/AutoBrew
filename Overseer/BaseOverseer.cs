using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal abstract class BaseOverseer
    {
        private protected static ManualLogSource Log => AutoBrewPlugin.Log;
        
        private OverseerStage _stage;
        private double _gtStart;
        private double _gtEnd;

        public bool Idle
        {
            get { return _stage == OverseerStage.Idle; }
        }

        public double Duration
        {
            get
            {
                if ((_stage != OverseerStage.Complete) && (_stage != OverseerStage.Failed))
                {
                    return 0.0;
                }
                return _gtEnd - _gtStart;
            }
        }

        public abstract double Accuracy { get; }

        public OverseerStage Stage
        {
            get { return _stage; }
            set
            {
                switch (value)
                {
                    case OverseerStage.Idle:
                    {
                        SetIdle();
                        return;
                    }
                    case OverseerStage.Active:
                    {
                        SetActive();
                        return;
                    }
                    case OverseerStage.Complete:
                    {
                        SetComplete();
                        return;
                    }
                    case OverseerStage.Failed:
                    {
                        SetFailed();
                        return;
                    }
                }
            }
        }

        public void Update(BrewOrder order)
        {
            if (Idle)
            {
                Setup(order);
            }

            Process();
            if (_stage == OverseerStage.Complete)
            {
                Log.LogInfo($"Order '{order.Stage}' completed in {Duration:N2}s, {Accuracy:P4} of target");
                BrewMaster.PrintRecipeMapMessage($"Order '{order.Stage}' completed", BrewMaster.OrderCompleteOffset);
                Reset();
                BrewMaster.AdvanceOrder();
            }
            else if (_stage == OverseerStage.Failed)
            {
                BrewMaster.LogFailedOrder(order);
                BrewMaster.Abort($"Brew cancelled - {order.Stage} failed");
            }
        }

        public abstract void Reconfigure(Dictionary<string, string> data);

        public abstract void Reset();

        public abstract void Setup(BrewOrder order);

        public abstract void Process();

        public abstract void LogStatus();

        private void SetIdle()
        {
            _stage = OverseerStage.Idle;
            _gtStart = 0.0;
            _gtEnd = 0.0;
        }

        private void SetActive()
        {
            _stage = OverseerStage.Active;
            _gtStart = Time.timeAsDouble;
        }

        private void SetComplete()
        {
            _gtEnd = Time.timeAsDouble;
            _stage = OverseerStage.Complete;
        }

        private void SetFailed()
        {
            _gtEnd = Time.timeAsDouble;
            _stage = OverseerStage.Failed;
        }
    }

    public enum OverseerStage
    {
        Idle,
        Active,
        Complete,
        Failed
    }
}
