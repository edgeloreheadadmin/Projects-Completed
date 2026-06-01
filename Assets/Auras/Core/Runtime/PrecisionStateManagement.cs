using UnityEngine;

namespace Auras.Core
{
    /// <summary>
    /// High-precision variant of layer state using doubles internally.
    /// Provides accumulation-resistant calculations for cascade operations.
    /// </summary>
    [System.Serializable]
    public struct AuraLayerStateHiPrecision
    {
        [SerializeField]
        [PrecisionLevel(PrecisionLevelAttribute.Precision.Double)]
        private double intensity;

        [SerializeField]
        [PrecisionLevel(PrecisionLevelAttribute.Precision.Double)]
        private double integrity;

        public double Intensity
        {
            get => intensity;
            set => intensity = Mathf.Clamp01((float)value);
        }

        public double Integrity
        {
            get => integrity;
            set => integrity = Mathf.Clamp01((float)value);
        }

        /// <summary>
        /// Compute energy as weighted blend of intensity (58%) and integrity (42%).
        /// Uses double precision to minimize accumulation error.
        /// </summary>
        public double GetEnergy()
        {
            return Mathf.Clamp01((float)((intensity * 0.58) + (integrity * 0.42)));
        }

        /// <summary>
        /// Compute integrity tint for color blending.
        /// Range: [0.24, 1.0] based on integrity value.
        /// </summary>
        public double GetIntegrityTint()
        {
            return Mathf.Clamp01(0.24f + ((float)integrity * 0.76f));
        }

        public AuraLayerStateHiPrecision(double intensity = 0.62, double integrity = 0.74)
        {
            this.intensity = Mathf.Clamp01((float)intensity);
            this.integrity = Mathf.Clamp01((float)integrity);
        }

        /// <summary>
        /// Convert to low-precision float state for rendering.
        /// </summary>
        public static implicit operator BiofieldLayerState(AuraLayerStateHiPrecision state)
        {
            return new BiofieldLayerState((float)state.intensity, (float)state.integrity);
        }

        /// <summary>
        /// Convert from low-precision state.
        /// </summary>
        public static implicit operator AuraLayerStateHiPrecision(BiofieldLayerState state)
        {
            return new AuraLayerStateHiPrecision(state.intensity, state.integrity);
        }
    }

    /// <summary>
    /// Manages smooth state transitions using easing curves.
    /// Replaces simple linear interpolation with configurable easing functions.
    /// Tracks blending progress to enable continuous smooth updates.
    /// </summary>
    public class StateTransitionManager
    {
        private double currentIntensity;
        private double currentIntegrity;
        private double targetIntensity;
        private double targetIntegrity;
        private double transitionProgress;

        public EasingCurve intensityEasing = EasingCurve.EaseInOutCubic();
        public EasingCurve integrityEasing = EasingCurve.EaseInOutCubic();

        /// <summary>
        /// Duration in seconds for state transitions.
        /// </summary>
        public double transitionDuration = 1.0;

        public double CurrentIntensity => currentIntensity;
        public double CurrentIntegrity => currentIntegrity;
        public double TargetIntensity => targetIntensity;
        public double TargetIntegrity => targetIntegrity;
        public double TransitionProgress => Mathf.Clamp01((float)transitionProgress);

        public StateTransitionManager(double initialIntensity = 0.62, double initialIntegrity = 0.74)
        {
            currentIntensity = Mathf.Clamp01((float)initialIntensity);
            currentIntegrity = Mathf.Clamp01((float)initialIntegrity);
            targetIntensity = currentIntensity;
            targetIntegrity = currentIntegrity;
            transitionProgress = 1.0;
        }

        /// <summary>
        /// Set new target state and initiate transition.
        /// </summary>
        public void SetTarget(double intensity, double integrity)
        {
            targetIntensity = Mathf.Clamp01((float)intensity);
            targetIntegrity = Mathf.Clamp01((float)integrity);
            transitionProgress = 0.0;
        }

        /// <summary>
        /// Update transition by deltaTime using easing curves.
        /// Returns true if transition is complete.
        /// </summary>
        public bool UpdateTransition(float deltaTime)
        {
            if (transitionProgress >= 1.0)
            {
                currentIntensity = targetIntensity;
                currentIntegrity = targetIntegrity;
                return true;
            }

            double step = deltaTime / transitionDuration;
            transitionProgress += step;

            if (transitionProgress >= 1.0)
            {
                transitionProgress = 1.0;
                currentIntensity = targetIntensity;
                currentIntegrity = targetIntegrity;
                return true;
            }

            double t = transitionProgress;
            currentIntensity = intensityEasing.InterpolateHighPrecision(
                currentIntensity, targetIntensity, t);
            currentIntegrity = integrityEasing.InterpolateHighPrecision(
                currentIntegrity, targetIntegrity, t);

            return false;
        }

        /// <summary>
        /// Apply current state immediately (skip transition).
        /// </summary>
        public void ApplyImmediate()
        {
            currentIntensity = targetIntensity;
            currentIntegrity = targetIntegrity;
            transitionProgress = 1.0;
        }

        /// <summary>
        /// Reset to initial state.
        /// </summary>
        public void Reset(double intensity = 0.62, double integrity = 0.74)
        {
            currentIntensity = Mathf.Clamp01((float)intensity);
            currentIntegrity = Mathf.Clamp01((float)integrity);
            targetIntensity = currentIntensity;
            targetIntegrity = currentIntegrity;
            transitionProgress = 1.0;
        }

        /// <summary>
        /// Get current state as high-precision struct.
        /// </summary>
        public AuraLayerStateHiPrecision GetCurrentState()
        {
            return new AuraLayerStateHiPrecision(currentIntensity, currentIntegrity);
        }

        /// <summary>
        /// Get target state as high-precision struct.
        /// </summary>
        public AuraLayerStateHiPrecision GetTargetState()
        {
            return new AuraLayerStateHiPrecision(targetIntensity, targetIntegrity);
        }
    }
}
