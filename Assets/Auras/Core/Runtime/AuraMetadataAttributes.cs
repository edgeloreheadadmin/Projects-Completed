using System;
using UnityEngine;

namespace Auras.Core
{
    /// <summary>
    /// Marks a layer interaction dependency in the cascade system.
    /// Replaces implicit weights with explicit, metadata-driven declarations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class LayerInteractionAttribute : Attribute
    {
        public string SourceLayerName { get; }
        public string TargetLayerName { get; }
        public float Strength { get; }
        public string EasingMode { get; }

        /// <summary>
        /// Declare a layer interaction dependency.
        /// </summary>
        /// <param name="sourceLayerName">Source layer enum name (e.g., "Emotional")</param>
        /// <param name="targetLayerName">Target layer enum name (e.g., "Mental")</param>
        /// <param name="strength">Interaction strength [0-1]</param>
        /// <param name="easingMode">Easing type: "Linear", "EaseInOut", "Cubic", "Bounce", "SpringPhysics", "CustomCurve"</param>
        public LayerInteractionAttribute(string sourceLayerName, string targetLayerName, float strength = 0.5f, string easingMode = "Linear")
        {
            SourceLayerName = sourceLayerName;
            TargetLayerName = targetLayerName;
            Strength = Mathf.Clamp01(strength);
            EasingMode = easingMode;
        }
    }

    /// <summary>
    /// Marks a field/property as using high-precision (double) calculations.
    /// Used internally to track which values need extra precision.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PrecisionLevelAttribute : Attribute
    {
        public enum Precision
        {
            Float,
            Double,
        }

        public Precision Level { get; }

        public PrecisionLevelAttribute(Precision level = Precision.Double)
        {
            Level = level;
        }
    }

    /// <summary>
    /// Specifies computation order for layer calculations in the dependency graph.
    /// Higher priority values are computed first.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    public sealed class ComputePhaseAttribute : Attribute
    {
        public int Priority { get; }

        public ComputePhaseAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }

    /// <summary>
    /// Marks a method/field as part of the cascade computation pipeline.
    /// Used for reflection-based binding and initialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    public sealed class CascadeBindingAttribute : Attribute
    {
        public string BindingKey { get; }

        public CascadeBindingAttribute(string bindingKey)
        {
            BindingKey = bindingKey;
        }
    }
}
