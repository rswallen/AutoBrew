using System;
using System.Collections.Generic;
using System.Text;

namespace AutoBrew
{
    internal readonly struct BrewOrderReport
    {
        public BrewOrderReport(double duration, double accuracy, double precision)
        {
            Duration = duration;
            Accuracy = accuracy;
            Precision = precision;
        }

        public readonly double Duration;
        public readonly double Accuracy;
        public readonly double Precision;
    }
}
