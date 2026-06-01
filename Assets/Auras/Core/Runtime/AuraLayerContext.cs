using System;
using System.Collections.Generic;
using UnityEngine;

namespace Auras.Core
{
    /// <summary>
    /// Encapsulates all state and configuration for a single aura layer.
    /// Provides type-safe, structured access to per-layer data instead of array indexing.
    /// Replaces BiofieldLayerState[] with a more organized, metadata-rich container.
    /// </summary>
    [System.Serializable]
    public class AuraLayerContext
    {
        /// <summary>
        /// Layer identifier (e.g., BiofieldLayerType.Emotional).
        /// Must be convertible to int for indexing.
        /// </summary>
        [SerializeField]
        public int layerId;

        [SerializeField]
        [PrecisionLevel(PrecisionLevelAttribute.Precision.Double)]
        private double currentIntensity;

        [SerializeField]
        [PrecisionLevel(PrecisionLevelAttribute.Precision.Double)]
        private double currentIntegrity;

        [SerializeField]
        [PrecisionLevel(PrecisionLevelAttribute.Precision.Double)]
        private double targetIntensity;

        [SerializeField]
        [PrecisionLevel(PrecisionLevelAttribute.Precision.Double)]
        private double targetIntegrity;

        /// <summary>
        /// Easing curve for smooth intensity transitions.
        /// </summary>
        [SerializeField]
        public EasingCurve intensityEasing = EasingCurve.EaseInOutCubic();

        /// <summary>
        /// Easing curve for smooth integrity transitions.
        /// </summary>
        [SerializeField]
        public EasingCurve integrityEasing = EasingCurve.EaseInOutCubic();

        /// <summary>
        /// Dependencies: which layers influence this one.
        /// </summary>
        [SerializeField]
        private List<LayerDependency> incomingDependencies = new();

        /// <summary>
        /// Custom metadata stored as key-value pairs.
        /// Useful for framework-specific data and attributes.
        /// </summary>
        [SerializeField]
        private Dictionary<string, string> metadata = new();

        /// <summary>
        /// Cached computed energy value (0-1 blend of intensity and integrity).
        /// Recalculated each frame during cascade computation.
        /// </summary>
        [SerializeField]
        [PrecisionLevel(PrecisionLevelAttribute.Precision.Double)]
        private double cachedEnergy;

        private bool energyDirty = true;

        public double CurrentIntensity
        {
            get => currentIntensity;
            set => currentIntensity = Mathf.Clamp01((float)value);
        }

        public double CurrentIntegrity
        {
            get => currentIntegrity;
            set => currentIntegrity = Mathf.Clamp01((float)value);
        }

        public double TargetIntensity
        {
            get => targetIntensity;
            set => targetIntensity = Mathf.Clamp01((float)value);
        }

        public double TargetIntegrity
        {
            get => targetIntegrity;
            set => targetIntegrity = Mathf.Clamp01((float)value);
        }

        /// <summary>
        /// Get the cached energy value, recomputing if dirty.
        /// Energy is a weighted blend of intensity (58%) and integrity (42%).
        /// </summary>
        public double GetEnergy()
        {
            if (energyDirty)
            {
                cachedEnergy = Mathf.Clamp01((float)((CurrentIntensity * 0.58) + (CurrentIntegrity * 0.42)));
                energyDirty = false;
            }

            return cachedEnergy;
        }

        public void InvalidateEnergy() => energyDirty = true;

        public IReadOnlyList<LayerDependency> IncomingDependencies => incomingDependencies.AsReadOnly();

        public void AddDependency(LayerDependency dependency)
        {
            if (dependency != null && !incomingDependencies.Contains(dependency))
            {
                incomingDependencies.Add(dependency);
            }
        }

        public void RemoveDependency(LayerDependency dependency)
        {
            incomingDependencies.Remove(dependency);
        }

        public void ClearDependencies()
        {
            incomingDependencies.Clear();
        }

        /// <summary>
        /// Set a metadata value.
        /// </summary>
        public void SetMetadata(string key, string value)
        {
            metadata[key] = value;
        }

        /// <summary>
        /// Try to get a metadata value.
        /// </summary>
        public bool TryGetMetadata(string key, out string value)
        {
            return metadata.TryGetValue(key, out value);
        }

        /// <summary>
        /// Get metadata value with a default fallback.
        /// </summary>
        public string GetMetadata(string key, string defaultValue = "")
        {
            return metadata.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public void ClearMetadata()
        {
            metadata.Clear();
        }

        /// <summary>
        /// Create a context for a given layer ID.
        /// </summary>
        public static AuraLayerContext Create(int layerId, double initialIntensity = 0.62, double initialIntegrity = 0.74)
        {
            var context = new AuraLayerContext
            {
                layerId = layerId,
                currentIntensity = initialIntensity,
                currentIntegrity = initialIntegrity,
                targetIntensity = initialIntensity,
                targetIntegrity = initialIntegrity,
            };
            context.InvalidateEnergy();
            return context;
        }
    }

    /// <summary>
    /// Represents a single dependency edge in the cascade graph.
    /// Specifies how one layer influences another.
    /// </summary>
    [System.Serializable]
    public class LayerDependency
    {
        [SerializeField]
        public int sourceLayerId;

        [SerializeField]
        public int targetLayerId;

        [SerializeField]
        [Range(0f, 1f)]
        public float strength = 0.5f;

        [SerializeField]
        public EasingCurve influenceCurve = EasingCurve.Linear();

        /// <summary>
        /// The metric to use: "intensity", "integrity", "energy", or custom metric.
        /// </summary>
        [SerializeField]
        public string sourceMetric = "intensity";

        /// <summary>
        /// Create a dependency from source to target layer.
        /// </summary>
        public LayerDependency(int sourceId, int targetId, float strength = 0.5f, string sourceMetric = "intensity")
        {
            this.sourceLayerId = sourceId;
            this.targetLayerId = targetId;
            this.strength = Mathf.Clamp01(strength);
            this.sourceMetric = sourceMetric;
        }

        public LayerDependency() { }
    }
}
