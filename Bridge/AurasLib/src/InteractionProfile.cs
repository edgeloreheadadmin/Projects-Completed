using System.Collections.Generic;
using Auras.Core;
using UnityEngine;

namespace Auras.BiofieldAuraMatrix
{
    /// <summary>
    /// Defines the layer interaction cascade graph for the Biofield framework.
    /// Replaces implicit hardcoded weights with explicit, editable dependencies.
    /// </summary>
    [CreateAssetMenu(fileName = "BiofieldInteractionProfile", menuName = "Auras/Biofield Aura Matrix/Interaction Profile")]
    public sealed class InteractionProfile : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Layer dependencies defining the cascade graph")]
        private List<LayerDependency> layerDependencies = new();

        [SerializeField]
        [Tooltip("Environment mode weights")]
        private BiofieldEnvironmentWeights environmentWeights = BiofieldEnvironmentWeights.GetDefault();

        [SerializeField]
        [Tooltip("Support input weights")]
        private BiofieldSupportWeights supportWeights = BiofieldSupportWeights.GetDefault();

        [SerializeField]
        [Tooltip("Layer input weights")]
        private BiofieldLayerWeights layerWeights = BiofieldLayerWeights.GetDefault();

        public IReadOnlyList<LayerDependency> LayerDependencies => layerDependencies.AsReadOnly();
        public BiofieldEnvironmentWeights EnvironmentWeights => environmentWeights;
        public BiofieldSupportWeights SupportWeights => supportWeights;
        public BiofieldLayerWeights LayerWeights => layerWeights;

        public void AddDependency(LayerDependency dependency)
        {
            if (dependency != null)
            {
                layerDependencies.Add(dependency);
            }
        }

        public void RemoveDependency(LayerDependency dependency)
        {
            layerDependencies.Remove(dependency);
        }

        public void ClearDependencies()
        {
            layerDependencies.Clear();
        }

        private void OnEnable()
        {
            if (layerDependencies == null)
            {
                layerDependencies = new List<LayerDependency>();
            }

            if (environmentWeights == null)
            {
                environmentWeights = BiofieldEnvironmentWeights.GetDefault();
            }

            if (supportWeights == null)
            {
                supportWeights = BiofieldSupportWeights.GetDefault();
            }

            if (layerWeights == null)
            {
                layerWeights = BiofieldLayerWeights.GetDefault();
            }
        }
    }

    /// <summary>
    /// Configurable weights for environmental mode influences.
    /// Extracted from hardcoded values in original ApplyInteractions().
    /// </summary>
    [System.Serializable]
    public class BiofieldEnvironmentWeights
    {
        [Range(0f, 1f)]
        public float circadianAlignment = 0.74f;

        [Range(0f, 1f)]
        public float socialSupport = 0.86f;

        [Range(0f, 1f)]
        public float informationClarity = 0.9f;

        [Range(0f, 1f)]
        public float groundingBias = 0.42f;

        public static BiofieldEnvironmentWeights GetDefault() => new();
    }

    /// <summary>
    /// Configurable weights for biofield support inputs.
    /// </summary>
    [System.Serializable]
    public class BiofieldSupportWeights
    {
        [Range(0f, 1f)]
        public float geomagneticPressure = 0.3f;

        [Range(0f, 1f)]
        public float natureExposure = 0.7f;

        [Range(0f, 1f)]
        public float energeticBoundaries = 0.72f;

        [Range(0f, 1f)]
        public float heartCoherence = 0.76f;

        [Range(0f, 1f)]
        public float contemplativeDepth = 0.72f;

        public static BiofieldSupportWeights GetDefault() => new();
    }

    /// <summary>
    /// Configurable weights for layer inputs.
    /// </summary>
    [System.Serializable]
    public class BiofieldLayerWeights
    {
        [Range(0f, 1f)]
        public float physicalEmbodiment = 0.74f;

        [Range(0f, 1f)]
        public float emotionalFluidity = 0.72f;

        [Range(0f, 1f)]
        public float cognitiveOrder = 0.74f;

        [Range(0f, 1f)]
        public float relationalOpenness = 0.76f;

        [Range(0f, 1f)]
        public float expressiveAlignment = 0.7f;

        [Range(0f, 1f)]
        public float intuitiveSensitivity = 0.78f;

        [Range(0f, 1f)]
        public float soulAlignment = 0.8f;

        public static BiofieldLayerWeights GetDefault() => new();
    }
}
