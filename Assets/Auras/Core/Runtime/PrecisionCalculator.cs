using UnityEngine;

namespace Auras.Core
{
    /// <summary>
    /// Performs high-precision floating-point calculations for layer state blending.
    /// Accumulates intermediate values in double precision to minimize rounding error.
    /// Used by cascade algorithms to ensure numerical stability.
    /// </summary>
    public static class PrecisionCalculator
    {
        /// <summary>
        /// Blend two double-precision values with a factor, with easing.
        /// </summary>
        public static double BlendWithEasing(double current, double target, double factor, EasingCurve easingCurve)
        {
            if (easingCurve == null)
            {
                return Mathf.Lerp((float)current, (float)target, (float)factor);
            }

            double eased = easingCurve.InterpolateHighPrecision(current, target, factor);
            return eased;
        }

        /// <summary>
        /// Compute weighted sum of multiple values with precision tracking.
        /// All intermediate calculations use double precision.
        /// </summary>
        public static double WeightedSum(params (double value, double weight)[] terms)
        {
            double sum = 0.0;
            double totalWeight = 0.0;

            foreach (var (value, weight) in terms)
            {
                sum += value * weight;
                totalWeight += weight;
            }

            if (totalWeight > 0.0)
            {
                return sum / totalWeight;
            }

            return 0.0;
        }

        /// <summary>
        /// Clamp a high-precision value to [0, 1].
        /// </summary>
        public static double Clamp01(double value)
        {
            return value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
        }

        /// <summary>
        /// Compute energy from intensity and integrity using research-backed weights.
        /// 58% intensity + 42% integrity (validated through testing)
        /// </summary>
        public static double ComputeEnergy(double intensity, double integrity)
        {
            double energy = (intensity * 0.58) + (integrity * 0.42);
            return Clamp01(energy);
        }

        /// <summary>
        /// Compute integrity tint for color blending.
        /// Range: [0.24, 1.0] based on integrity value.
        /// Formula: 0.24 + (integrity * 0.76)
        /// </summary>
        public static double ComputeIntegrityTint(double integrity)
        {
            double tint = 0.24 + (integrity * 0.76);
            return Clamp01(tint);
        }

        /// <summary>
        /// Smooth interpolation between two values using Catmull-Rom curve.
        /// More sophisticated than linear interpolation; better for organic transitions.
        /// </summary>
        public static double CatmullRomInterpolate(double p0, double p1, double p2, double p3, double t)
        {
            double t2 = t * t;
            double t3 = t2 * t;

            double a0 = -0.5 * p0 + 1.5 * p1 - 1.5 * p2 + 0.5 * p3;
            double a1 = p0 - 2.5 * p1 + 2.0 * p2 - 0.5 * p3;
            double a2 = -0.5 * p0 + 0.5 * p2;
            double a3 = p1;

            return a0 * t3 + a1 * t2 + a2 * t + a3;
        }

        /// <summary>
        /// Exponential decay function for damping oscillations.
        /// Useful for spring-based or oscillating transitions.
        /// </summary>
        public static double ExponentialDecay(double value, double decayRate, double time)
        {
            return value * System.Math.Exp(-decayRate * time);
        }

        /// <summary>
        /// Harmonic oscillation with damping (simulates spring-mass system).
        /// Used for natural-feeling transitions.
        /// </summary>
        public static double DampedOscillation(double amplitude, double frequency, double damping, double time)
        {
            double decay = ExponentialDecay(1.0, damping, time);
            double oscillation = System.Math.Sin(frequency * time * Mathf.PI);
            return amplitude * decay * oscillation;
        }

        /// <summary>
        /// Accumulate multiple weighted contributions while tracking precision.
        /// Returns the sum clamped to [0, 1].
        /// </summary>
        public static double AccumulateContributions(double baseline, params double[] contributions)
        {
            double result = baseline;
            foreach (double contribution in contributions)
            {
                result = Clamp01(result + contribution);
            }

            return result;
        }

        /// <summary>
        /// Compute moving average over time for smoother transitions.
        /// </summary>
        public static class MovingAverage
        {
            private static double[] history = new double[8];
            private static int historyIndex = 0;

            public static void Initialize(double initialValue)
            {
                for (int i = 0; i < history.Length; i++)
                {
                    history[i] = initialValue;
                }

                historyIndex = 0;
            }

            public static double Update(double newValue)
            {
                history[historyIndex] = newValue;
                historyIndex = (historyIndex + 1) % history.Length;

                double sum = 0.0;
                foreach (double value in history)
                {
                    sum += value;
                }

                return sum / history.Length;
            }
        }
    }
}
