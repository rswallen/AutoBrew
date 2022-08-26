using System;
using System.Collections.Generic;
using System.Text;

namespace AutoBrew.Overseer
{
    internal class DebugOverseer : BaseOverseer
    {
        private BrewOrder _order;
        
        public override void Reconfigure(Dictionary<string, string> data) { }

        public override void Reset()
        {
            _order = null;

            Stage = OverseerStage.Idle;
        }

        public override void Setup(BrewOrder order)
        {
            _order = order;

            Stage = OverseerStage.Active;
        }

        public override void Process()
        {
            Log.LogInfo($"DebugOverseer can't process '{_order.Stage}' so terminates");
            Stage = OverseerStage.Complete;
        }

        public override void LogStatus()
        {
            Log.LogInfo($"DebugOverseer is wondering why it was ever made to process order '{_order.Stage}'");
        }

        public override double Accuracy
        {
            get
            {
                return 9.001;
            }
        }

    }
}
