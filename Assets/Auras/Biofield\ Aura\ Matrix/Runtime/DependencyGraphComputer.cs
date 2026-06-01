using System;
using System.Collections.Generic;
using System.Linq;
using Auras.Core;
using UnityEngine;

namespace Auras.BiofieldAuraMatrix
{
    /// <summary>
    /// Computes layer states based on explicit dependency graph.
    /// Replaces the 80-line implicit weight calculation in original ApplyInteractions().
    /// Provides topology-ordered computation for cascade effects.
    /// </summary>
    public class DependencyGraphComputer
    {
        private readonly List<LayerDependency> dependencies;
        private readonly LayerContextCollection contexts;
        private readonly BiofieldAuraProfile profile;
        private List<int> computationOrder;

        /// <summary>
        /// Events for debugging and visualization.
        /// </summary>
        public event Action<int, string> OnLayerComputed;
        public event Action<LayerDependency> OnDependencyEvaluated;

        public DependencyGraphComputer(LayerContextCollection contexts, BiofieldAuraProfile profile, List<LayerDependency> dependencies)
        {
            this.contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.dependencies = dependencies ?? new List<LayerDependency>();

            BuildComputationOrder();
        }

        /// <summary>
        /// Build topology-ordered list of layers for cascade computation.
        /// Layers with no dependencies computed first, then dependents.
        /// </summary>
        private void BuildComputationOrder()
        {
            computationOrder = new List<int>();
            var allLayers = new HashSet<int>();
            var computed = new HashSet<int>();
            var inProgress = new HashSet<int>();

            foreach (var ctx in contexts.GetAll())
            {
                allLayers.Add(ctx.layerId);
            }

            foreach (int layerId in allLayers.OrderBy(l => l))
            {
                DepthFirstSearch(layerId, computed, inProgress);
            }

            computationOrder = computationOrder.Distinct().ToList();
        }

        private void DepthFirstSearch(int layerId, HashSet<int> computed, HashSet<int> inProgress)
        {
            if (computed.Contains(layerId))
            {
                return;
            }

            if (inProgress.Contains(layerId))
            {
                Debug.LogWarning($"Circular dependency detected at layer {layerId}");
                return;
            }

            inProgress.Add(layerId);

            var incomingDeps = dependencies.Where(d => d.targetLayerId == layerId).ToList();
            foreach (var dep in incomingDeps)
            {
                if (!computed.Contains(dep.sourceLayerId))
                {
                    DepthFirstSearch(dep.sourceLayerId, computed, inProgress);
                }
            }

            inProgress.Remove(layerId);
            computationOrder.Add(layerId);
            computed.Add(layerId);
        }

        /// <summary>
        /// Compute all layer states based on dependency graph.
        /// Returns the computed states as a dictionary.
        /// </summary>
        public Dictionary<int, BiofieldLayerState> ComputeLayerStates(float environmentModifier = 1f)
        {
            var states = new Dictionary<int, BiofieldLayerState>();

            if (computationOrder == null || computationOrder.Count == 0)
            {
                BuildComputationOrder();
            }

            foreach (int layerId in computationOrder)
            {
                var context = contexts.Get(layerId);
                if (context == null)
                {
                    continue;
                }

                BiofieldLayerState state = ComputeLayerState(layerId, states, environmentModifier);
                states[layerId] = state;

                OnLayerComputed?.Invoke(layerId, $"Computed state: I={state.intensity:F3}, G={state.integrity:F3}");
            }

            return states;
        }

        /// <summary>
        /// Compute state for a single layer based on its dependencies.
        /// </summary>
        private BiofieldLayerState ComputeLayerState(int layerId, Dictionary<int, BiofieldLayerState> computedStates, float environmentModifier)
        {
            var context = contexts.Get(layerId);
            if (context == null)
            {
                return new BiofieldLayerState(0.5f, 0.5f);
            }

            double intensityAccumulator = context.TargetIntensity;
            double integrityAccumulator = context.TargetIntegrity;

            var incomingDeps = dependencies.Where(d => d.targetLayerId == layerId).ToList();

            foreach (var dep in incomingDeps)
            {
                if (!computedStates.TryGetValue(dep.sourceLayerId, out var sourceState))
                {
                    continue;
                }

                double sourceValue = GetSourceMetricValue(sourceState, dep.sourceMetric);
                double influence = ApplyInfluenceCurve(sourceValue, dep);
                double weightedInfluence = influence * dep.strength * environmentModifier;

                intensityAccumulator = PrecisionCalculator.Clamp01(intensityAccumulator + (weightedInfluence * 0.5));
                integrityAccumulator = PrecisionCalculator.Clamp01(integrityAccumulator + (weightedInfluence * 0.5));

                OnDependencyEvaluated?.Invoke(dep);
            }

            AuraStateValidator.EnforceConstraints(context);

            return new BiofieldLayerState((float)intensityAccumulator, (float)integrityAccumulator);
        }

        /// <summary>
        /// Get a metric value from a layer state.
        /// </summary>
        private double GetSourceMetricValue(BiofieldLayerState state, string metric)
        {
            return metric?.ToLower() switch
            {
                "intensity" => state.intensity,
                "integrity" => state.integrity,
                "energy" => (state.intensity * 0.58) + (state.integrity * 0.42),
                _ => state.intensity,
            };
        }

        /// <summary>
        /// Apply influence curve to a source value.
        /// Allows nonlinear influence modeling.
        /// </summary>
        private double ApplyInfluenceCurve(double sourceValue, LayerDependency dependency)
        {
            if (dependency.influenceCurve == null)
            {
                return sourceValue;
            }

            return dependency.influenceCurve.Evaluate((float)sourceValue);
        }

        /// <summary>
        /// Add a dependency dynamically at runtime.
        /// Triggers recomputation of topology order.
        /// </summary>
        public void AddDependency(LayerDependency dependency)
        {
            dependencies.Add(dependency);
            BuildComputationOrder();
            Debug.Log($"Added dependency: Layer {dependency.sourceLayerId} → {dependency.targetLayerId}");
        }

        /// <summary>
        /// Remove a dependency.
        /// </summary>
        public void RemoveDependency(LayerDependency dependency)
        {
            dependencies.Remove(dependency);
            BuildComputationOrder();
            Debug.Log($"Removed dependency: Layer {dependency.sourceLayerId} → {dependency.targetLayerId}");
        }

        /// <summary>
        /// Remove all dependencies for a layer.
        /// </summary>
        public void RemoveDependenciesForLayer(int layerId)
        {
            dependencies.RemoveAll(d => d.targetLayerId == layerId || d.sourceLayerId == layerId);
            BuildComputationOrder();
        }

        /// <summary>
        /// Get all dependencies affecting a layer.
        /// </summary>
        public List<LayerDependency> GetIncomingDependencies(int layerId)
        {
            return dependencies.Where(d => d.targetLayerId == layerId).ToList();
        }

        /// <summary>
        /// Get all layers this layer influences.
        /// </summary>
        public List<LayerDependency> GetOutgoingDependencies(int layerId)
        {
            return dependencies.Where(d => d.sourceLayerId == layerId).ToList();
        }

        /// <summary>
        /// Debug: Print the computation order to console.
        /// </summary>
        public void DebugPrintComputationOrder()
        {
            Debug.Log("Dependency Graph Computation Order:");
            for (int i = 0; i < computationOrder.Count; i++)
            {
                Debug.Log($"  {i + 1}. Layer {computationOrder[i]}");
            }
        }

        /// <summary>
        /// Debug: Visualize the dependency graph.
        /// Returns a string representation for logging/debugging.
        /// </summary>
        public string VisualizeDependencyGraph()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Dependency Graph Visualization ===");

            var layerIds = contexts.GetAll().Select(c => c.layerId).OrderBy(id => id).ToList();

            foreach (int layerId in layerIds)
            {
                var inDeps = GetIncomingDependencies(layerId);
                var outDeps = GetOutgoingDependencies(layerId);

                sb.AppendLine($"\nLayer {layerId}:");

                if (inDeps.Count == 0)
                {
                    sb.AppendLine("  ← (no incoming)");
                }
                else
                {
                    foreach (var dep in inDeps)
                    {
                        sb.AppendLine($"  ← Layer {dep.sourceLayerId} (strength: {dep.strength:F2}, metric: {dep.sourceMetric})");
                    }
                }

                if (outDeps.Count == 0)
                {
                    sb.AppendLine("  → (no outgoing)");
                }
                else
                {
                    foreach (var dep in outDeps)
                    {
                        sb.AppendLine($"  → Layer {dep.targetLayerId}");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
