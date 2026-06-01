using UnityEngine;

namespace Auras.Core
{
    /// <summary>
    /// Validates aura layer states to prevent invalid combinations and ensure numerical stability.
    /// Used by cascade algorithms to verify constraints before and after computations.
    /// </summary>
    public static class AuraStateValidator
    {
        /// <summary>
        /// Validate that a state value is within valid range [0, 1].
        /// </summary>
        public static bool IsValidRange(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0.0 && value <= 1.0;
        }

        /// <summary>
        /// Validate an entire layer state for correctness.
        /// </summary>
        public static bool ValidateState(AuraLayerContext context)
        {
            if (context == null)
            {
                return false;
            }

            return IsValidRange(context.CurrentIntensity) &&
                   IsValidRange(context.CurrentIntegrity) &&
                   IsValidRange(context.TargetIntensity) &&
                   IsValidRange(context.TargetIntegrity);
        }

        /// <summary>
        /// Validate all states in a collection.
        /// </summary>
        public static bool ValidateCollection(LayerContextCollection collection)
        {
            if (collection == null || collection.Count == 0)
            {
                return false;
            }

            foreach (var context in collection.GetAll())
            {
                if (!ValidateState(context))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Enforce state constraints: clamp to [0, 1] and handle edge cases.
        /// </summary>
        public static void EnforceConstraints(AuraLayerContext context)
        {
            if (context == null)
            {
                return;
            }

            context.CurrentIntensity = Mathf.Clamp01((float)context.CurrentIntensity);
            context.CurrentIntegrity = Mathf.Clamp01((float)context.CurrentIntegrity);
            context.TargetIntensity = Mathf.Clamp01((float)context.TargetIntensity);
            context.TargetIntegrity = Mathf.Clamp01((float)context.TargetIntegrity);
            context.InvalidateEnergy();
        }

        /// <summary>
        /// Enforce constraints on all contexts in a collection.
        /// </summary>
        public static void EnforceCollectionConstraints(LayerContextCollection collection)
        {
            if (collection == null)
            {
                return;
            }

            collection.ForEach(EnforceConstraints);
        }

        /// <summary>
        /// Check if a state transition is valid (no NaN or infinity).
        /// </summary>
        public static bool ValidateTransition(double from, double to, double progress)
        {
            if (!IsValidRange(from) || !IsValidRange(to) || !IsValidRange(progress))
            {
                return false;
            }

            double interpolated = Mathf.Lerp((float)from, (float)to, (float)progress);
            return IsValidRange(interpolated);
        }

        /// <summary>
        /// Detect and handle numerical instability (values diverging or becoming extreme).
        /// </summary>
        public static bool DetectInstability(AuraLayerContext context)
        {
            if (context == null)
            {
                return true;
            }

            double intensity = context.CurrentIntensity;
            double integrity = context.CurrentIntegrity;

            if (double.IsNaN(intensity) || double.IsInfinity(intensity))
            {
                Debug.LogWarning($"Intensity instability detected: {intensity}");
                context.CurrentIntensity = 0.5;
                return true;
            }

            if (double.IsNaN(integrity) || double.IsInfinity(integrity))
            {
                Debug.LogWarning($"Integrity instability detected: {integrity}");
                context.CurrentIntegrity = 0.5;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Debug helper: compare two states and report differences.
        /// </summary>
        public static void ReportStateDifference(AuraLayerContext state1, AuraLayerContext state2, double threshold = 0.01)
        {
            if (state1 == null || state2 == null)
            {
                return;
            }

            if (System.Math.Abs(state1.CurrentIntensity - state2.CurrentIntensity) > threshold)
            {
                Debug.Log($"Intensity diff: {state1.CurrentIntensity} vs {state2.CurrentIntensity}");
            }

            if (System.Math.Abs(state1.CurrentIntegrity - state2.CurrentIntegrity) > threshold)
            {
                Debug.Log($"Integrity diff: {state1.CurrentIntegrity} vs {state2.CurrentIntegrity}");
            }
        }
    }
}
