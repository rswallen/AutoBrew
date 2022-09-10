using UnityEngine;

namespace AutoBrew
{
    public class PIDController
    {
        private readonly double _Kp;
        private readonly double _Ki;
        private readonly double _Kd;

        public double Propotional { get; private set; }
        public double Integral { get; private set; }
        public double Derivative { get; private set; }
        public double LastError { get; private set; }

        public PIDController(float Kp, float Ki, float Kd)
        {
            _Kp = Kp;
            _Ki = Ki;
            _Kd = Kd;

            Propotional = 0f;
            Integral = 0f;
            Derivative = 0f;
            LastError = 0f;
        }

        public PIDController(Vector3 constants) : this(constants.x, constants.y, constants.z) { }

        public double GetStep(double setpoint, double actual, double deltaTime)
        {
            LastError = Propotional;
            Propotional = setpoint - actual;
            Integral += Propotional * deltaTime;
            Derivative = (Propotional - LastError) / deltaTime;
            
            return (Propotional * _Kp) + (Integral * _Ki) + (Derivative * _Kd);
        }
    }
}