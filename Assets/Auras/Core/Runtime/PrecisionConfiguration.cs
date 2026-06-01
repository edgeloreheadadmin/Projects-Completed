using UnityEngine;

namespace Auras.Core
{
    /// <summary>
    /// Centralized configuration for high-precision calculations and easing behavior.
    /// Allows fine-tuning of numerical accuracy and transition smoothness.
    /// </summary>
    [CreateAssetMenu(fileName = "PrecisionConfiguration", menuName = "Auras/Core/Precision Configuration")]
    public sealed class PrecisionConfiguration : ScriptableObject
    {
        [Header("Numerical Precision")]
        [SerializeField]
        [Tooltip("Enable high-precision (double) calculations internally")]
        private bool enableHighPrecision = true;

        [SerializeField]
        [Tooltip("Minimum difference threshold before updating a value (prevents jitter)")]
        [Range(0.0001f, 0.01f)]
        private float updateThreshold = 0.001f;

        [SerializeField]
        [Tooltip("Enable validation and constraint checking each frame")]
        private bool enableStateValidation = true;

        [Header("Transition Behavior")]
        [SerializeField]
        [Tooltip("Default duration for layer state transitions (seconds)")]
        [Range(0.01f, 5f)]
        private float defaultTransitionDuration = 0.5f;

        [SerializeField]
        [Tooltip("Default easing curve type for all transitions")]
        private EasingType defaultEasingType = EasingType.EaseInOutCubic;

        [SerializeField]
        [Tooltip("Enable smooth animation curves between states")]
        private bool enableSmoothTransitions = true;

        [Header("Spring Physics (if using SpringPhysics easing)")]
        [SerializeField]
        [Range(0.1f, 10f)]
        private float springStiffness = 3f;

        [SerializeField]
        [Range(0f, 1f)]
        private float springDamping = 0.6f;

        [Header("Stability")]
        [SerializeField]
        [Tooltip("Clamp extreme values to prevent numerical overflow")]
        private bool clampExtremeValues = true;

        [SerializeField]
        [Tooltip("If true, recover from NaN/infinity by resetting to default")]
        private bool autoRecoverFromInstability = true;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("Recovery value when instability is detected")]
        private float recoveryValue = 0.5f;

        public bool EnableHighPrecision => enableHighPrecision;
        public float UpdateThreshold => updateThreshold;
        public bool EnableStateValidation => enableStateValidation;
        public float DefaultTransitionDuration => defaultTransitionDuration;
        public EasingType DefaultEasingType => defaultEasingType;
        public bool EnableSmoothTransitions => enableSmoothTransitions;
        public float SpringStiffness => springStiffness;
        public float SpringDamping => springDamping;
        public bool ClampExtremeValues => clampExtremeValues;
        public bool AutoRecoverFromInstability => autoRecoverFromInstability;
        public float RecoveryValue => recoveryValue;

        /// <summary>
        /// Create default configuration with recommended settings.
        /// </summary>
        public static PrecisionConfiguration CreateDefault()
        {
            var config = CreateInstance<PrecisionConfiguration>();
            config.name = "Default Precision Configuration";
            return config;
        }

        /// <summary>
        /// Get a preset configuration optimized for smooth, responsive transitions.
        /// </summary>
        public static PrecisionConfiguration GetResponsivePreset()
        {
            var config = CreateDefault();
            config.defaultTransitionDuration = 0.2f;
            config.defaultEasingType = EasingType.EaseInOutQuad;
            config.springStiffness = 5f;
            config.springDamping = 0.5f;
            return config;
        }

        /// <summary>
        /// Get a preset optimized for smooth, gradual transitions.
        /// </summary>
        public static PrecisionConfiguration GetSmoothPreset()
        {
            var config = CreateDefault();
            config.defaultTransitionDuration = 1.0f;
            config.defaultEasingType = EasingType.EaseInOutCubic;
            config.springStiffness = 2f;
            config.springDamping = 0.7f;
            return config;
        }

        /// <summary>
        /// Get a preset optimized for spring-based natural motion.
        /// </summary>
        public static PrecisionConfiguration GetSpringPreset()
        {
            var config = CreateDefault();
            config.defaultTransitionDuration = 0.6f;
            config.defaultEasingType = EasingType.SpringPhysics;
            config.springStiffness = 4f;
            config.springDamping = 0.4f;
            return config;
        }

        private void OnValidate()
        {
            if (updateThreshold < 0.0001f)
            {
                updateThreshold = 0.0001f;
            }

            if (defaultTransitionDuration < 0.01f)
            {
                defaultTransitionDuration = 0.01f;
            }
        }
    }
}
