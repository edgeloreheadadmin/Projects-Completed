using System.Collections.Generic;
using Auras.Core;
using UnityEngine;

namespace Auras.IntegrativeAuraFramework
{
    /// <summary>
    /// Profile for the Integrative Aura Framework.
    /// Stores layer definitions and interaction metadata.
    /// </summary>
    [CreateAssetMenu(menuName = "Auras/Integrative Aura Matrix/Integration Profile", fileName = "IntegrativeAuraProfile")]
    public sealed class IntegrativeAuraProfile : ScriptableObject
    {
        [SerializeField]
        private List<IntegrativeLayerDefinition> layerDefinitions = new List<IntegrativeLayerDefinition>();

        [SerializeField]
        private InteractionProfile interactionProfile;

        public List<IntegrativeLayerDefinition> LayerDefinitions => layerDefinitions;
        public InteractionProfile InteractionProfile => interactionProfile;

        public IntegrativeLayerDefinition GetLayer(IntegrativeLayerType layer)
        {
            int index = (int)layer;
            if (index >= 0 && index < layerDefinitions.Count)
            {
                return layerDefinitions[index];
            }
            return null;
        }

        public static IntegrativeLayerDefinition GetDefaultLayerDefinition(IntegrativeLayerType layer)
        {
            var def = new IntegrativeLayerDefinition { layer = layer, displayName = layer.ToString() };
            return def;
        }

        [ContextMenu("Reset to Research Defaults")]
        public void ResetToResearchDefaults()
        {
            layerDefinitions.Clear();

            var colors = new[]
            {
                new Color(1f, 0f, 0f), // Red - Physical
                new Color(1f, 0.5f, 0f), // Orange - Vitality
                new Color(1f, 1f, 0f), // Yellow - Emotional
                new Color(0f, 0f, 1f), // Blue - Mental
                new Color(0.5f, 0f, 1f), // Violet - Transcendent
            };

            var descriptions = new[]
            {
                "Physical body and manifest reality",
                "Life force and vital energy",
                "Emotional states and feelings",
                "Thoughts and mental processes",
                "Spirit and transcendent awareness",
            };

            for (int i = 0; i < (int)IntegrativeLayerType.Count; i++)
            {
                var def = GetDefaultLayerDefinition((IntegrativeLayerType)i);
                def.displayName = ((IntegrativeLayerType)i).ToString();
                def.aspectDescription = descriptions[i];
                def.balancedColor = colors[i];
                def.baseScale = 1f + (i * 0.1f);
                def.peakScale = 1.3f + (i * 0.1f);
                def.baseEmission = 4f + (i * 2f);
                def.peakEmission = 18f + (i * 3f);
                def.pulseSpeed = 0.8f + (i * 0.1f);

                layerDefinitions.Add(def);
            }
        }

        public void EnsureStructure()
        {
            if (layerDefinitions.Count < (int)IntegrativeLayerType.Count)
            {
                while (layerDefinitions.Count < (int)IntegrativeLayerType.Count)
                {
                    var index = layerDefinitions.Count;
                    var def = GetDefaultLayerDefinition((IntegrativeLayerType)index);
                    def.displayName = ((IntegrativeLayerType)index).ToString();
                    layerDefinitions.Add(def);
                }
            }
        }
    }
}
