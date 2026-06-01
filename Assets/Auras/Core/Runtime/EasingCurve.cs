using UnityEngine;

namespace Auras.Core
{
    /// <summary>
    /// Flexible easing system supporting predefined curves, custom AnimationCurves, and spring physics.
    /// Replaces simple linear interpolation with smooth, configurable transitions.
    /// </summary>
    public enum EasingType
    {
        Linear,
        EaseInOutQuad,
        EaseInOutCubic,
        EaseInOutQuart,
        Bounce,
        SpringPhysics,
        CustomCurve,
    }

    /// <summary>
    /// Encapsulates an easing function with its configuration.
    /// Supports predefined curves, custom animation curves, and spring physics.
    /// </summary>
    [System.Serializable]
    public class EasingCurve
    {
        [SerializeField]
        public EasingType type = EasingType.Linear;

        [SerializeField]
        [Tooltip("For CustomCurve easing type")]
        public AnimationCurve customCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [SerializeField]
        [Tooltip("Spring stiffness for SpringPhysics easing [0.1-10]")]
        [Range(0.1f, 10f)]
        public float springStiffness = 3f;

        [SerializeField]
        [Tooltip("Spring damping for SpringPhysics easing [0-1]")]
        [Range(0f, 1f)]
        public float springDamping = 0.6f;

        /// <summary>
        /// Evaluate easing function at normalized time t [0-1].
        /// Returns eased value, typically in range [0-1] but may overshoot with certain curves.
        /// </summary>
        public float Evaluate(float t)
        {
            t = Mathf.Clamp01(t);

            return type switch
            {
                EasingType.Linear => t,
                EasingType.EaseInOutQuad => EaseInOutQuad(t),
                EasingType.EaseInOutCubic => EaseInOutCubic(t),
                EasingType.EaseInOutQuart => EaseInOutQuart(t),
                EasingType.Bounce => EaseBounce(t),
                EasingType.SpringPhysics => EaseSpring(t),
                EasingType.CustomCurve => customCurve.Evaluate(t),
                _ => t,
            };
        }

        /// <summary>
        /// Interpolate from current to target using this easing curve.
        /// </summary>
        public float Interpolate(float current, float target, float t)
        {
            float eased = Evaluate(t);
            return Mathf.Lerp(current, target, eased);
        }

        /// <summary>
        /// High-precision version using double internally.
        /// </summary>
        public double InterpolateHighPrecision(double current, double target, double t)
        {
            double eased = Evaluate((float)Mathf.Clamp01((float)t));
            return current + (target - current) * eased;
        }

        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : -1f + 4f * t - 2f * t * t;
        }

        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        private static float EaseInOutQuart(float t)
        {
            return t < 0.5f
                ? 8f * t * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;
        }

        private static float EaseBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }

            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }

            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }

            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        private float EaseSpring(float t)
        {
            if (t >= 1f) return 1f;

            float frequency = springStiffness;
            float damping = Mathf.Clamp01(springDamping);

            return 1f - Mathf.Exp(-damping * frequency * t) * Mathf.Cos(frequency * t * Mathf.PI);
        }

        public static EasingCurve Linear() => new() { type = EasingType.Linear };
        public static EasingCurve EaseInOutCubic() => new() { type = EasingType.EaseInOutCubic };
        public static EasingCurve Bounce() => new() { type = EasingType.Bounce };
        public static EasingCurve Spring(float stiffness = 3f, float damping = 0.6f) =>
            new() { type = EasingType.SpringPhysics, springStiffness = stiffness, springDamping = damping };
        public static EasingCurve Custom(AnimationCurve curve) =>
            new() { type = EasingType.CustomCurve, customCurve = curve };
    }
}
