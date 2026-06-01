using System.Collections.Generic;
using Auras.Core;
using UnityEngine;

namespace Auras.BiofieldAuraMatrix
{
    /// <summary>
    /// Debugging and visualization tools for the dependency graph.
    /// Provides real-time visualization, state monitoring, and performance metrics.
    /// </summary>
    public class DependencyGraphDebugger : MonoBehaviour
    {
        [SerializeField]
        private BiofieldAuraDriverV2 driver;

        [SerializeField]
        private BiofieldAuraController controller;

        [SerializeField]
        private bool visualizeGraph = false;

        [SerializeField]
        private bool monitorStates = false;

        [SerializeField]
        private bool showPerformanceMetrics = false;

        private Dictionary<int, double> previousStates = new Dictionary<int, double>();
        private double lastComputeTime = 0.0;
        private int frameCount = 0;

        private void OnEnable()
        {
            if (driver == null)
            {
                driver = GetComponent<BiofieldAuraDriverV2>();
            }

            if (controller == null)
            {
                controller = GetComponent<BiofieldAuraController>();
            }
        }

        private void Update()
        {
            if (!Application.isEditor)
            {
                return;
            }

            frameCount++;

            if (visualizeGraph)
            {
                VisualizeGraph();
            }

            if (monitorStates)
            {
                MonitorStates();
            }

            if (showPerformanceMetrics && frameCount % 30 == 0)
            {
                LogPerformanceMetrics();
            }
        }

        /// <summary>
        /// Visualize the dependency graph in the editor.
        /// Shows layer interconnections and influence paths.
        /// </summary>
        private void VisualizeGraph()
        {
            if (controller == null)
            {
                return;
            }

            var contextCollection = controller.GetComponent<Auras.Core.LayerContextCollection>();
            if (contextCollection == null)
            {
                return;
            }

            foreach (var context in contextCollection.GetAll())
            {
                var pos = transform.position + Vector3.up * (context.layerId * 0.5f);

                float energy = (float)context.GetEnergy();
                Color layerColor = Color.Lerp(Color.red, Color.green, energy);

                Debug.DrawLine(pos, pos + Vector3.forward * 0.2f, layerColor, Time.deltaTime);
            }
        }

        /// <summary>
        /// Monitor state changes frame-by-frame.
        /// Logs significant changes in layer states.
        /// </summary>
        private void MonitorStates()
        {
            if (controller == null)
            {
                return;
            }

            const double threshold = 0.05;

            foreach (BiofieldLayerType layer in System.Enum.GetValues(typeof(BiofieldLayerType)))
            {
                var context = controller.GetContext(layer);
                if (context == null)
                {
                    continue;
                }

                int layerId = (int)layer;
                double currentEnergy = context.GetEnergy();

                if (previousStates.TryGetValue(layerId, out double prevEnergy))
                {
                    double delta = System.Math.Abs(currentEnergy - prevEnergy);
                    if (delta > threshold)
                    {
                        Debug.Log($"Layer {layer}: Energy {prevEnergy:F3} → {currentEnergy:F3} (Δ{delta:F3})");
                    }
                }

                previousStates[layerId] = currentEnergy;
            }
        }

        /// <summary>
        /// Log performance metrics for the cascade computation.
        /// </summary>
        private void LogPerformanceMetrics()
        {
            var metrics = new System.Text.StringBuilder();
            metrics.AppendLine("=== Dependency Graph Performance ===");
            metrics.AppendLine($"Frame: {Time.frameCount}");
            metrics.AppendLine($"Last Compute Time: {lastComputeTime:F4}ms");

            var contextCollection = controller.GetComponent<Auras.Core.LayerContextCollection>();
            if (contextCollection != null)
            {
                metrics.AppendLine($"Total Layers: {contextCollection.Count}");

                int validStates = 0;
                foreach (var context in contextCollection.GetAll())
                {
                    if (AuraStateValidator.ValidateState(context))
                    {
                        validStates++;
                    }
                }

                metrics.AppendLine($"Valid States: {validStates}/{contextCollection.Count}");
            }

            Debug.Log(metrics.ToString());
        }

        /// <summary>
        /// Export state snapshot for analysis.
        /// </summary>
        [ContextMenu("Export State Snapshot")]
        public void ExportStateSnapshot()
        {
            if (controller == null)
            {
                Debug.LogError("Controller not assigned");
                return;
            }

            var snapshot = new System.Text.StringBuilder();
            snapshot.AppendLine("=== Layer State Snapshot ===");
            snapshot.AppendLine($"Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

            foreach (BiofieldLayerType layer in System.Enum.GetValues(typeof(BiofieldLayerType)))
            {
                var context = controller.GetContext(layer);
                if (context == null)
                {
                    continue;
                }

                snapshot.AppendLine($"\n{layer}:");
                snapshot.AppendLine($"  Intensity: {context.CurrentIntensity:F4} (target: {context.TargetIntensity:F4})");
                snapshot.AppendLine($"  Integrity: {context.CurrentIntegrity:F4} (target: {context.TargetIntegrity:F4})");
                snapshot.AppendLine($"  Energy: {context.GetEnergy():F4}");
            }

            Debug.Log(snapshot.ToString());
        }

        /// <summary>
        /// Reset all layers to neutral state.
        /// </summary>
        [ContextMenu("Reset All Layers")]
        public void ResetAllLayers()
        {
            if (controller != null)
            {
                controller.ResetToNeutral();
            }
        }

        /// <summary>
        /// Test layer influence by boosting one layer and observing cascades.
        /// </summary>
        [ContextMenu("Test: Boost Etheric Layer")]
        public void TestBoostEtheric()
        {
            if (driver != null)
            {
                driver.SetLayerInput(BiofieldLayerType.Etheric, 1f);
                driver.ApplyInteractionsNow();
            }
        }

        [ContextMenu("Test: Boost Emotional Layer")]
        public void TestBoostEmotional()
        {
            if (driver != null)
            {
                driver.SetLayerInput(BiofieldLayerType.Emotional, 1f);
                driver.ApplyInteractionsNow();
            }
        }

        [ContextMenu("Test: Boost Celestial Layer")]
        public void TestBoostCelestial()
        {
            if (driver != null)
            {
                driver.SetLayerInput(BiofieldLayerType.Celestial, 1f);
                driver.ApplyInteractionsNow();
            }
        }
    }
}
